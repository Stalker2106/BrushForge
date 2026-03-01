extends Node

var rawEntities; # Array of all BSPv30 entities in sledge format

var targets; # Map of connected signals between entities;

var globals; # Map of entities with globalname;

func _init() -> void:
    targets = {};
    globals = {};

func addTarget(targetName: String, node: Node):
    if targets.has(targetName):
        targets[targetName].push_back(node);
    else:
        targets[targetName] = [node];

func addGlobal(globalName: String, node: Node):
    globals[globalName] = node;

func dumpGlobals():
    for glob in globals:
        remove_child(globals[glob]);
    return globals;

func importGlobals(prevGlobals: Dictionary) -> void:
    for key in prevGlobals:
        if globals.has(key):
            globals[key].active = prevGlobals[key].active;
        else:
            globals[key] = prevGlobals[key];

func getTargets(targetName: String) -> Array:
    if targets.has(targetName):
        return targets[targetName];
    return [];

func getTexture(textureName: String) -> Texture2D:
    var tree = Engine.get_main_loop();
    var files = tree.root.get_node("App").files;
    for file in files:
        if file.type != "Pack" || file.format != "WAD3":
            continue; # Not a WAD
        if file && file.gdTextures.has(textureName):
            return file.gdTextures[textureName];
    var tex = load("res://sprites/NotFound.png");
    tex.resource_name = "MissingTexture";
    return tex;

func bakeLighting():
    var giNode = get_node_or_null("GI");
    # Add GI if missing
    if !giNode:
        var levelMesh = get_node("Model0/Mesh") as MeshInstance3D;
        var aabb = levelMesh.global_transform * levelMesh.mesh.get_aabb();
        giNode = VoxelGI.new();
        giNode.name = "GI";
        giNode.position = aabb.get_center();
        giNode.size = aabb.size;
        giNode.visible = false;
        add_child(giNode);
    giNode.bake();
