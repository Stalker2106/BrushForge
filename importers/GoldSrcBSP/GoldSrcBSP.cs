using Godot;
using Godot.Collections;
using System;
using System.IO;
using Sledge.Formats.Bsp;
using Sledge.Formats.Bsp.Objects;
using Sledge.Formats.Id;

public partial class GoldSrcBSP : DataPack
{
    //1 is world, 2 are player world colliders, 4 (8 in bin) are projectiles
    const uint WorldCollisionLayer = 1;
    const uint PlayerCollisionLayer = 2;
    const uint ProjectilesCollisionLayer = 8;
    
    private string skyName;
    private Vector3 startOrigin;

    private Array<GEntity> entities;
    private ComputedFace[] faces;
    public BspFile bsp;
    public Dictionary<int, String> modelsClassName;
    
    public Godot.Collections.Dictionary<string, Texture2D> gdTextures;

    override public Array<Variant> GetLevels()
    {
        return new Array<Variant>() { GetFileName() };
    }

    public Vector3 GetLevelStart()
    {
        return Vector3.Zero;
    }

    // We have to populate surfacetool per triangle (3 vertex for each),
    // (1)  In triangle strips (/\/\/\), we iterate on current summit + the next two verts, because they are also part of the triangle.
    // (-1) In triangle fans (_\|/_), we iterate on first summit + the next two verts, because all triangles have the first summit of
    //      the whole array in common.
    // In both cases, we end with an offset of 2 to compensate.
    // We store the index of current summit in triangle fan vertex array in the "trivertIndex" variable
    static public int GetSummitVertIndex(int trianglePackType, int packIndex, int summit)
    {
        int vertIndex = -1;
        if (trianglePackType == 1)
        {
            switch (summit)
            {
                case 0:
                    vertIndex = packIndex + 0;
                    break;
                case 1:
                    vertIndex = packIndex % 2 == 1 ? packIndex + 2 : packIndex + 1;
                    break;
                case 2:
                    vertIndex = packIndex % 2 == 1 ? packIndex + 1 : packIndex + 2;
                    break;
            }
        }
        else
        {
            vertIndex = (summit == 0 ? 0 : packIndex + summit);
        }
        return vertIndex;
    }

    public partial class LightmapBuilder : GodotObject
    {
        private const int BytesPerPixel = 3;

        public int Width { get; }
        public int Height { get; private set; }
        public System.Drawing.Rectangle FullbrightRectangle { get; }

        private byte[] _data;
        public byte[] Data => _data;

        private int _currentX;
        private int _currentY;
        private int _currentRowHeight;

        public LightmapBuilder(int initialWidth = 256, int initialHeight = 32)
        {
            _data = new byte[initialWidth * initialHeight * BytesPerPixel];
            Width = initialWidth;
            Height = initialHeight;
            _currentX = 0;
            _currentY = 0;
            _currentRowHeight = 2;

            // (0, 0) is fullbright
            FullbrightRectangle = Allocate(1, 1, new[] { byte.MaxValue, byte.MaxValue, byte.MaxValue }, 0);
        }

        public System.Drawing.Rectangle Allocate(int width, int height, byte[] data, int index)
        {
            if (_currentX + width > Width) NewRow();
            if (_currentY + height > Height) Expand();

            for (var i = 0; i < height; i++)
            {
                var start = (_currentY + i) * (Width * BytesPerPixel) + _currentX * BytesPerPixel;
                System.Array.Copy(data, width * i * BytesPerPixel + index, _data, start, width * BytesPerPixel);
            }

            var x = _currentX;
            var y = _currentY;

            _currentX += width + 2;
            _currentRowHeight = Math.Max(_currentRowHeight, height + 2);

            return new System.Drawing.Rectangle(x, y, width, height);
        }

        private void NewRow()
        {
            _currentX = 0;
            _currentY += _currentRowHeight;
            _currentRowHeight = 2;
        }

        private void Expand()
        {
            Height *= 2;
            System.Array.Resize(ref _data, Width * Height * BytesPerPixel);
        }

        public void AddLuminosity(int luminosity)
        {
            for (int i = 0; i < _data.Length; i++)
            {
                _data[i] = (Byte)(_data[i] + luminosity > 255 ? 255 : _data[i] + luminosity);
            }
        }
    }

    public partial class ComputedFace : GodotObject
    {
        public Face bspFace;

        public Vector2[] faceUVs;
        public Vector2[] lightmapUVs;

        public Texture2D lightmapTexture;

        public System.Drawing.Rectangle alloc;

        public ComputedFace(BspFile bsp, int faceId, LightmapBuilder builder)
        {
            bspFace = bsp.Faces[faceId];
            // No lightmap
            //if (bspFace.LightmapOffset < 0 || bspFace.Styles[0] == byte.MaxValue) return;

            Vector2[] rawUVs = new Vector2[bspFace.NumEdges];
            faceUVs = new Vector2[bspFace.NumEdges];
            Vector2 fmins = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 fmaxs = new Vector2(float.MinValue, float.MinValue);

            // Generate texture coordinates for face
            TextureInfo texinfo = bsp.Texinfo[bspFace.TextureInfo];
            MipTexture texture = bsp.Textures[texinfo.MipTexture];
            for (int edgeN = 0; edgeN < bspFace.NumEdges; edgeN++)
            {
                int surfedge = bsp.Surfedges[bspFace.FirstEdge + edgeN];
                Edge edge = bsp.Edges[Math.Abs(surfedge)];
                Vector3 vertex = bsp.Vertices[surfedge > 0 ? edge.Start : edge.End].ToGodotVector3();
                rawUVs[edgeN] = new Vector2(
                    vertex.Dot(texinfo.S.ToGodotVector3()) + texinfo.S.W,
                    vertex.Dot(texinfo.T.ToGodotVector3()) + texinfo.T.W
                );
                // For faces, we need to apply texture scale to uvs.
                faceUVs[edgeN] = new Vector2(rawUVs[edgeN].X / texture.Width, rawUVs[edgeN].Y / texture.Height);
                // We then extract min and max out of the floored values
                if (rawUVs[edgeN].X < fmins.X) fmins.X = rawUVs[edgeN].X;
                if (rawUVs[edgeN].X > fmaxs.X) fmaxs.X = rawUVs[edgeN].X;
                if (rawUVs[edgeN].Y < fmins.Y) fmins.Y = rawUVs[edgeN].Y;
                if (rawUVs[edgeN].Y > fmaxs.Y) fmaxs.Y = rawUVs[edgeN].Y;
            }

            // Compute lightmap size
            var fcmaxs = (fmaxs / 16.0f).Ceil();
            var ffmins = (fmins / 16.0f).Floor();
            Vector2I lightmapSize = ((Vector2I)fcmaxs - (Vector2I)ffmins) + new Vector2I(1, 1);

            if (bspFace.LightmapOffset < 0 || bspFace.LightmapOffset >= bsp.Lightmaps.RawLightmapData.Length || bspFace.Styles[0] == byte.MaxValue)
            {
                // fullbright
                alloc = builder.FullbrightRectangle;
            }
            else
            {
                // Load texture from rawLightmap
                alloc = builder.Allocate(lightmapSize.X, lightmapSize.Y, bsp.Lightmaps.RawLightmapData, bspFace.LightmapOffset);
            }

            // Compute lightmap UV
            lightmapUVs = new Vector2[bspFace.NumEdges];
            for (int edge = 0; edge < bspFace.NumEdges; edge++)
            {
                lightmapUVs[edge] = (rawUVs[edge] - fmins) / (fmaxs - fmins);
            }
        }

        /* build trifan */
        public Vector3[] BuildTriFanVertices(BspFile bsp)
        {
            Vector3[] triFanVertices = new Vector3[bspFace.NumEdges];
            for (int edgeN = 0; edgeN < bspFace.NumEdges; edgeN++)
            {
                int surfedge = bsp.Surfedges[bspFace.FirstEdge + edgeN];
                Edge edge = bsp.Edges[Math.Abs(surfedge)];
                triFanVertices[edgeN] = bsp.Vertices[surfedge >= 0 ? edge.Start : edge.End].ToSGodotVector3();
            }
            return triFanVertices;
        }

        public void Final(LightmapBuilder builder)
        {
            var lightmapWidth = builder.Width;
            var lightmapHeight = builder.Height;
            // Compute lightmap UV
            for (int edge = 0; edge < bspFace.NumEdges; edge++)
            {
                lightmapUVs[edge] = new Vector2(
                    (alloc.X + 0.5f) / lightmapWidth + (lightmapUVs[edge].X * (alloc.Width - 1)) / lightmapWidth,
                    (alloc.Y + 0.5f) / lightmapHeight + (lightmapUVs[edge].Y * (alloc.Height - 1)) / lightmapHeight
                );
            }
        }
    }

    public ComputedFace GetFaceFromTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        foreach (ComputedFace face in faces)
        {
            int found = 0;
            for (int edgeN = 0; edgeN < face.bspFace.NumEdges; edgeN++)
            {
                int surfedge = bsp.Surfedges[face.bspFace.FirstEdge + edgeN];
                Edge edge = bsp.Edges[Math.Abs(surfedge)];
                Vector3 vertex = bsp.Vertices[surfedge > 0 ? edge.Start : edge.End].ToGodotVector3();
                if (vertex.IsEqualApprox(v1) || vertex.IsEqualApprox(v2) || vertex.IsEqualApprox(v3))
                    found += 1;
            }
            if (found == 3)
                return face;
        }
        return null;
    }

    override public void Import(FileStream fs)
    {
        GD.Print("Parsing BSPv30 file...");
        bsp = new BspFile(fs);
        gdTextures = new Godot.Collections.Dictionary<string, Texture2D>();

        entities = new Array<GEntity>();
        for (int ent = 0; ent < bsp.Entities.Count; ent++)
        {
            entities.Add(new GEntity(bsp.Entities[ent]));
        }

        // Build Face UVs & lightmap
        GD.Print("Building lightmap...");
        LightmapBuilder builder = new LightmapBuilder();
        faces = new ComputedFace[bsp.Faces.Count];
        for (int faceId = 0; faceId < bsp.Faces.Count; faceId++)
        {
            faces[faceId] = new ComputedFace(bsp, faceId, builder);
        }
        builder.AddLuminosity(50);
        var builderTex = ImageTexture.CreateFromImage(Image.CreateFromData(builder.Width, builder.Height, false, Image.Format.Rgb8, builder.Data));
        gdTextures["Lightmap"] = builderTex;
        for (int faceId = 0; faceId < faces.Length; faceId++)
        {
            faces[faceId].Final(builder);
            faces[faceId].lightmapTexture = builderTex;
        }
    }
    
    //
    // Build on Godot
    //
    
    public Node3D CreateBodyNode(ArrayMesh mesh, bool meshVisible, uint collisionLayers, System.Numerics.Vector3 mins, System.Numerics.Vector3 maxs)
    {
        // Create node
        StaticBody3D bodyNode = new StaticBody3D();
        bodyNode.CollisionLayer = collisionLayers; 
        bodyNode.CollisionMask = collisionLayers;
        if (mesh != null) {
            // Generate mesh
            MeshInstance3D brushMesh = new MeshInstance3D();
            brushMesh.Mesh = mesh;
            brushMesh.Name = "Mesh";
            brushMesh.Visible = meshVisible;
            bodyNode.AddChild(brushMesh);
            // Generate Collision
            CollisionShape3D shape = new CollisionShape3D();
            shape.Name = "CollisionShape";
            ConcavePolygonShape3D trimesh = mesh.CreateTrimeshShape();
            trimesh.BackfaceCollision = true;
            shape.Shape = trimesh;
            bodyNode.AddChild(shape);
        } else {
            //If mesh is missing we build from mins/maxs
            BoxShape3D bboxShape = new BoxShape3D();
            bboxShape.Size = new Vector3(maxs.X - mins.X, maxs.Y - mins.Y, maxs.Z - mins.Z).ToSGodotVector3().Abs();
            CollisionShape3D shape = new CollisionShape3D();
            shape.Name = "CollisionShape";
            shape.Shape = bboxShape;
            bodyNode.AddChild(shape);
        }
        return bodyNode;
    }

    public Dictionary<string, SurfaceTool> BuildModelFaces(Node3D mapNode, int modelIdx, Vector3 modelCenter)
    {
        // Create transform
        Transform3D modelTransform = new Transform3D(Basis.Identity, modelCenter);
        // Build model
        StandardMaterial3D NotFoundMaterial = GD.Load<StandardMaterial3D>("res://materials/NotFoundMaterial.tres");
        Dictionary<string, SurfaceTool> surfaceTools = new Dictionary<string, SurfaceTool>();
        int faceEnd = bsp.Models[modelIdx].FirstFace + bsp.Models[modelIdx].NumFaces;
        Array<string> missingTextures = new Array<string>();
        for (int faceIdx = bsp.Models[modelIdx].FirstFace; faceIdx < faceEnd; faceIdx++)
        {
            ComputedFace face = faces[(int)faceIdx];
            TextureInfo texinfo = bsp.Texinfo[face.bspFace.TextureInfo];
            MipTexture texture = null;
            String faceTextureName = Convert.ToString(texinfo.MipTexture);
            if (texinfo.MipTexture < bsp.Textures.Count)
            {
                // In older versions of BSP, there is no texture
                texture = bsp.Textures[(int)texinfo.MipTexture];
                // We force uppercase for texture names in both BSP / WAD
                faceTextureName = texture.Name.ToUpper();
            }
            // Create one surfaceTool per texture
            SurfaceTool surfaceTool;
            if (surfaceTools.ContainsKey(faceTextureName))
            {
                surfaceTool = surfaceTools[faceTextureName];
            }
            else
            {
                // Create faces materials
                StandardMaterial3D material = new StandardMaterial3D();
                material.MetallicSpecular = 0;
                Texture2D faceTexture = mapNode.Call("getTexture", faceTextureName).As<Texture2D>();
                if (faceTexture.ResourceName != "MissingTexture")
                {
                    material.AlbedoTexture = faceTexture;
                    material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear;
                    material.DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Burley;
                    material.ShadingMode = StandardMaterial3D.ShadingModeEnum.PerPixel;
                    material.CullMode = BaseMaterial3D.CullModeEnum.Back;
                    if (modelsClassName.ContainsKey(modelIdx) && modelsClassName[modelIdx] == "func_wall")
                    {
                        material.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
                    }
                    // Emissive material
                    /*if (lightsRad.ContainsKey(faceTextureName))
                    {
                        var radData = lightsRad[faceTextureName].As<Godot.Collections.Dictionary>();
                        material.EmissionEnabled = true;
                        material.EmissionTexture = faceTexture;
                        material.EmissionOperator = BaseMaterial3D.EmissionOperatorEnum.Multiply;
                        material.Emission = radData["color"].As<Color>();
                        material.EmissionEnergyMultiplier = radData["intensity"].As<float>();
                    }*/
                }
                else
                {
                    material.AlbedoTexture = NotFoundMaterial.AlbedoTexture;
                    if (!missingTextures.Contains(faceTextureName))
                        missingTextures.Add(faceTextureName);
                }
                material.DetailEnabled = true;
                material.DetailBlendMode = BaseMaterial3D.BlendModeEnum.Mul;
                material.DetailAlbedo = face.lightmapTexture;
                material.DetailUVLayer = BaseMaterial3D.DetailUV.UV2;
                surfaceTool = new SurfaceTool();
                surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
                surfaceTool.SetMaterial(material.Duplicate() as Material);
                surfaceTools[faceTextureName] = surfaceTool;
            }
            // Compute triangles from face vertices
            Vector3[] triFanVertices = face.BuildTriFanVertices(bsp);
            //We send the surface to sftool
            for (int i = 0; i < triFanVertices.Length - 2; i++)
            {
                for (int summit = 0; summit < 3; summit++)
                {
                    int trivertIndex = GetSummitVertIndex(-1, i, summit);
                    surfaceTool.SetUV(face.faceUVs[trivertIndex]);
                    surfaceTool.SetUV2(face.lightmapUVs[trivertIndex]);
                    surfaceTool.SetNormal(bsp.Planes[face.bspFace.Plane].Normal.ToSGodotVector3());
                    surfaceTool.AddVertex(triFanVertices[trivertIndex] * modelTransform);
                }
            }
        }
        foreach (var missingTex in missingTextures) {
            //Missing tex
        }
        return surfaceTools;
    }
    
    public Node3D BuildGDLevel(string levelPath, Godot.Collections.Array<Asset> assets)
    {
        try {
            PreParseEntities(levelPath, assets);
            // Level
            Node3D mapNode = new Node3D();
            mapNode.Name = "Map";
            mapNode.SetScript(GD.Load<Script>("res://scripts/Level.gd"));
            mapNode.Set("rawEntities", entities);
            mapNode.Set("wads", assets);
            ArrayMesh skyMesh = null;
            for (int modelIdx = 0; modelIdx < bsp.Models.Count; modelIdx++) {
                var anims = new Godot.Collections.Array<Godot.Node>();
                // Compute BBOX
                var mins = bsp.Models[modelIdx].Mins;
                var maxs = bsp.Models[modelIdx].Maxs;
                Vector3 modelCenter = Vector3.Zero;
                if (modelIdx == 0)
                    modelCenter = mins.ToSGodotVector3().Lerp(maxs.ToSGodotVector3(), 0.5f);
                // Build surfaces
                Dictionary<string, SurfaceTool> surfaceTools = BuildModelFaces(mapNode, modelIdx, modelCenter);
                // Start building model tree
                ArrayMesh modelMesh = null;
                var surfaceToolTextures = new Godot.Collections.Array<string>(surfaceTools.Keys);
                for (int surfaceIdx = 0; surfaceIdx < surfaceToolTextures.Count; ++surfaceIdx)
                {
                    var textureName = surfaceToolTextures[surfaceIdx];
                    surfaceTools[textureName].GenerateTangents();
                    // We extract Sky in a separate mesh
                    if (textureName == "sky")
                        skyMesh = surfaceTools[textureName].Commit(skyMesh);
                    else
                        modelMesh = surfaceTools[textureName].Commit(modelMesh);
                    // Handle animated textures if needed
                    if (textureName.Contains("+"))
                        HandleAnimatedTextures(anims, mapNode, textureName, surfaceIdx);
                }
                // Generate model mesh
                uint collisionLayers = WorldCollisionLayer | PlayerCollisionLayer;
                if (modelsClassName.ContainsKey(modelIdx) && modelsClassName[modelIdx] == "func_wall") {
                    // If is a func_wall, bullets go through
                } else {
                        collisionLayers = collisionLayers | ProjectilesCollisionLayer;
                }
                Node3D modelNode = CreateBodyNode(modelMesh, true, collisionLayers, mins, maxs);
                modelNode.Name = "Model" + modelIdx.ToString();
                modelNode.Position = modelCenter;
                mapNode.AddChild(modelNode);
                // Add anims
                foreach (var anim in anims) {
                    modelNode.AddChild(anim);
                }
            }
            // Generate sky mesh if necessary
            if (skyMesh != null) {
                Node3D skyNode = CreateBodyNode(skyMesh, false, PlayerCollisionLayer, System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero);
                skyNode.Name = "Sky";
                mapNode.AddChild(skyNode);
            }
            // Parse entities
            ParseEntities(mapNode, assets);
            // All done!
            return mapNode;
        } catch (Exception e) {
            GD.Print(e);
            return null;
        }
    }

    public void HandleAnimatedTextures(Godot.Collections.Array<Godot.Node> anims, Node3D mapNode, string textureName, int surfaceIdx)
    {
        int plusIndex = textureName.IndexOf('+');
        // Base name: "+0door" -> "door"
        string baseName = textureName.Substring(plusIndex + 2);
        // Collect frames (+0..+9)
        var frames = new Godot.Collections.Array<Texture2D>();
        for (char c = '0'; c <= '9'; c++)
        {
            string name = $"+{c}{baseName}";
            Texture2D tex = mapNode.Call("getTexture", name).As<Texture2D>();
            if (tex.ResourceName == "MissingTexture") break;
            frames.Add(tex);
        }
        // Multi frame, animation required
        if (frames.Count > 1)
        {
            Godot.Node animationNode = new Godot.Node();
            animationNode.Name = "Surface"+surfaceIdx+"Anim";
            animationNode.SetScript(GD.Load<Script>("res://materials/AnimatedMaterial.gd"));
            animationNode.Call("configure", frames, surfaceIdx);
            anims.Add(animationNode);
        }
    }

    public void PreParseEntities(string levelPath, Godot.Collections.Array<Asset> assets)
    {
        // Preparse
        modelsClassName = new Dictionary<int, String>();
        foreach (GEntity entity in entities)
        {
            // Entity has a internal use
            if (entity.ClassName != "")
            {
                // Contains BSP generic data
                if (entity.ClassName == "worldspawn")
                {
                    if (entity.Get<string>("skyname", null) != null)
                    {
                        skyName = entity.Get<string>("skyname", null);
                    }
                    if (entity.Get<string>("wad", null) != null)
                    {
                        // Get the level directory path
                        string levelFolder = Path.GetDirectoryName(levelPath);
                        if (!string.IsNullOrEmpty(levelFolder))
                        {
                            levelFolder = levelFolder.Replace("\\", "/"); // Convert backslashes to forward slashes if needed
                            if (!levelFolder.EndsWith("/"))
                            {
                                levelFolder += "/";
                            }
                        }
                        // Inject wads specified in bsp
                        string[] additionalWads = entity.Get<string>("wad", null).Split(';') ?? new string[0];
                        foreach (string wadPath in additionalWads)
                        {
                            var localPath = levelFolder+Path.GetFileName(wadPath.Replace("\\", "/"));
                            Asset loadedWad = null; //Importer.ImportRawWad(localPath);
                            if (loadedWad != null)
                            {
                                
                                assets.Add(loadedWad);
                            }
                        }
                    }
                }
                // Entity is not a point, register class
                else if (entity.Get<string>("model", "").StartsWith("*"))
                {
                    int modelNumber = int.Parse(entity.Get<string>("model", null).Replace("*", ""));
                    modelsClassName[modelNumber] = entity.ClassName;
                }
            }
        }
    }

    public void ParseEntities(Node3D mapNode, Godot.Collections.Array<Asset> assets)
    {
        // Parse entities, lets do magic
        Dictionary<string, GEntity> targets = new Dictionary<string, GEntity>();
        var entityId = 0;
        foreach (GEntity entity in entities)
        {
            Node3D entityNode = null;
            // Entity has a internal use
            if (entity.ClassName != "")
            {
                // Brush Entities
                if (entity.Get<string>("model", "").StartsWith("*"))
                {
                    if (entity.ClassName.StartsWith("func")) {
                        entityNode = Entities.ParseFuncs(entity, mapNode);
                    }
                    else if (entity.ClassName.StartsWith("trigger")) {
                        entityNode = Entities.ParseBrushTriggers(entity, mapNode);
                    }
                    else if (entityNode != null) {
                        entityNode.SetMeta("unsupported", true);
                    }
                    MeshInstance3D modelMesh = entityNode.GetNodeOrNull("Mesh") as MeshInstance3D;
                    if (entityNode != null && entityNode.HasMeta("unsupported"))
                    {
                        //This is an unsupported entity, so we drop collider if any and hide mesh
                        if (entityNode != null) {
                            entityNode.SetMeta("unsupported", true);
                            var collider = entityNode.GetNode("CollisionShape");
                            if (collider != null)
                                collider.QueueFree();
                            if (modelMesh != null)
                                modelMesh.Visible = false;
                        }
                    }
                    // Add highlight shader
                    if (modelMesh != null) {
                        var shader = GD.Load<ShaderMaterial>("res://materials/OutlineMaterial.tres");
                        for (int surfIdx = 0; surfIdx < modelMesh.Mesh.GetSurfaceCount(); surfIdx++) {
                            var mat = modelMesh.Mesh.SurfaceGetMaterial(surfIdx);
                            mat.NextPass = shader;
                        }
                    }
                }
                else
                {
                    if (entity.ClassName.StartsWith("light"))
                    {
                        entityNode = Entities.ParseLights(entity);
                    }
                    else if (entity.ClassName.StartsWith("info"))
                    {
                        entityNode = Entities.ParseInfos(entity, mapNode);
                    }
                    else if (entity.ClassName.StartsWith("env"))
                    {
                        entityNode = Entities.ParseEnvs(entity, mapNode);
                    }
                    else if (entity.ClassName.StartsWith("path"))
                    {
                        entityNode = Entities.ParsePaths(entity, mapNode);
                    }
                    else if (entity.ClassName.StartsWith("trigger"))
                    {
                        entityNode = Entities.ParsePointTriggers(entity, mapNode);
                    }
                    else if (entity.ClassName.StartsWith("multi_manager"))
                    {
                        entityNode = new Node3D();
                        entityNode.SetScript(GD.Load<Script>("res://entities/MultiManager.gd"));
                        var fields = entity.GetFields();
                        fields.Remove("classname");
                        fields.Remove("origin");
                        fields.Remove("targetname");
                        entityNode.Call("configure", fields);
                    }
                    else if (entity.ClassName.StartsWith("spawn_deathmatch"))
                    {
                        entityNode = new Node3D();
                    }
                    if (entityNode == null)
                    {
                        //This is an unsupported entity, so we drop collider if any and hide mesh
                        entityNode = new Node3D();
                        entityNode.SetMeta("unsupported", true);
                    }
                    // We place the point entities at appropriate location here
                    string[] vecs = entity.Get<string>("origin", "0 0 0").Replace(".", ",").Split(" ");
                    entityNode.Position = new Vector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])).ToSGodotVector3();
                    entityNode.Name = entity.ClassName+"_"+entityId;
                    mapNode.AddChild(entityNode);
                    // Add gizmo
                    if (true) {
                        // Add collision for debug ray
                        StaticBody3D gizmo = new StaticBody3D();
                        gizmo.Name = entityNode.Name;
                        CollisionShape3D shape = new CollisionShape3D();
                        SphereShape3D sShape = new SphereShape3D();
                        sShape.Radius = 0.25f;
                        shape.Shape = sShape;
                        gizmo.AddChild(shape);
                        // Add sprite
                        Sprite3D gizmoSprite = new Sprite3D();
                        string iconPath = "res://sprites/entityicons/" + entity.ClassName + ".png";
                        Texture2D icon;
                        if (Godot.FileAccess.FileExists(iconPath))
                            icon = GD.Load<Texture2D>(iconPath);
                        else
                            icon = GD.Load<Texture2D>("res://sprites/entityicons/info_null.png");
                        gizmoSprite.SetTexture(icon);
                        gizmoSprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
                        gizmoSprite.PixelSize = 0.005f;
                        gizmo.AddChild(gizmoSprite);
                        entityNode.AddChild(gizmo);
                    }
                }
            }
            if (entityNode == null)
                continue; //This is a broken entity
            // If entity has a targetname register globally
            var targetName = entity.Get<string>("targetname", null);
            if (targetName != null && !entityNode.HasMeta("unsupported"))
            {
                mapNode.Call("addTarget", targetName, entityNode);
                entityNode.Name = targetName;
            }
            // If entity has a globalname register globally
            var globalName = entity.Get<string>("globalname", null);
            if (globalName != null && !entityNode.HasMeta("unsupported"))
            {
                mapNode.Call("addGlobal", globalName, entityNode);
            }
            entityId += 1;
        }
    }
}
