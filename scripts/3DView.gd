extends Control

const CameraPrefab = preload("res://prefabs/Camera.tscn");

var settings;
var shading;
var camera;
var worldContainer;
var crosshair;

var currentLevelMetadata;
var currentLevelFileId;
var currentLevelNode;

# Called when the node enters the scene tree for the first time.
func _ready():
    currentLevelFileId = null;
    shading = "texturized";
    settings = {
        "render": {
            "skybox": false,
            "lights": true,
            "pointEntities": true,
            "brushEntities": true
        },
        "entities": {
            "playAudio": true
        },
        "camera": {
            "collisions": false,
            #"gravity": false
        }
    };
    get_node("/root/App/Layout/CenterLayout/Main/Top/TitleBar/").setButtonStates(settings);
    # Nodes
    crosshair = get_node("../Crosshair");
    worldContainer = get_node("World/Container");
    camera = get_node("World/Camera");
    connect("gui_input", Callable(self, "viewInput"));
    # Toolbar
    var sidebar = get_node("/root/App/Layout/CenterLayout/Main/Views/Sidebar/TopRight/Shading");
    sidebar.get_node("WireframeBtn").connect("pressed", Callable(self, "setShading").bind("wireframe"));
    sidebar.get_node("ShadedBtn").connect("pressed", Callable(self, "setShading").bind("shaded"));
    sidebar.get_node("TexturizedBtn").connect("pressed", Callable(self, "setShading").bind("texturized"));

func _input(event):
    if event.is_action_pressed("Escape"):
        setMouseCapture(false);

func _physics_process(delta):
    if camera:
        var coord = "Origin: %.02f %.02f  %.02f" % [camera.get_position().x, camera.get_position().y, camera.get_position().z]
        get_node("/root/App/Layout/CenterLayout/Main/Views/Sidebar/CoordinatesLabel").set_text(coord);

func viewInput(event):
    if event is InputEventMouseButton && event.pressed:
        setMouseCapture(true);

func setMouseCapture(enabled):
    if enabled:
        Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED);
        crosshair.visible = true;
    else:
        Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE);
        crosshair.visible = false;

func setSetting(section, key, value):
    if settings[section][key] == value:
        return; #Already set
    settings[section][key] = value;
    applySettings();
    
func setShading(shading_):
    shading = shading_;
    reloadLevel();

func applySettings():
    if !currentLevelNode:
        return;
    var skyMesh = currentLevelNode.get_node_or_null("Skybox");
    if skyMesh:
        skyMesh.visible = !settings.render.skybox;
    var skyMat = camera.get_node("Camera").environment.sky.sky_material;
    if settings.render.skybox && false:
        var files = get_node("/root/App").files;
        var cubemapTextures = files[currentLevelFileId].GetSkyCubemapTextures(files);
        skyMat.set_shader_parameter("front", cubemapTextures[0]);
        skyMat.set_shader_parameter("left", cubemapTextures[1]);
        skyMat.set_shader_parameter("back", cubemapTextures[2]);
        skyMat.set_shader_parameter("right", cubemapTextures[3]);
        skyMat.set_shader_parameter("top", cubemapTextures[4]);
        skyMat.set_shader_parameter("bottom", cubemapTextures[5]);
    else:
        var blackPixel = load("res://sprites/blackpixel.png");
        skyMat.set_shader_parameter("front", blackPixel);
        skyMat.set_shader_parameter("left", blackPixel);
        skyMat.set_shader_parameter("back", blackPixel);
        skyMat.set_shader_parameter("right", blackPixel);
        skyMat.set_shader_parameter("top", blackPixel);
        skyMat.set_shader_parameter("bottom", blackPixel);
    for entity in currentLevelNode.get_children():
        if entity is OmniLight3D:
            entity.visible = settings.render.lights;
        #if entity is Track:
        #    if settings.render.tracks
        if entity is CollisionObject3D:
            entity.visible = settings.render.brushEntities;
        else:
            if entity is not OmniLight3D && entity is not SpotLight3D:
                entity.visible = settings.render.pointEntities;
    camera.get_node("CameraCollider").disabled = !settings.camera.collisions;
    #camera.gravity = settings.camera.gravity;

func hasLevelLoaded():
    return currentLevelNode != null;

func getLevelMetadata():
    return currentLevelMetadata;

func reloadLevel():
    if currentLevelFileId == null:
        return;
    if hasLevelLoaded():
        unload();
    var levelFile = get_node("/root/App").files[currentLevelFileId];
    currentLevelNode = levelFile.BuildGDLevel(get_node("/root/App").files, shading);
    if (currentLevelNode == null):
        print("Failed to load map");
        return;
    worldContainer.add_child(currentLevelNode);
    applySettings();
    #get_node("Viewport/World/VoxelGI").bake(get_node("Viewport/World/Container"));

func loadNextMap():
    var levels = getConnectedLevels();
    print(levels);

func getConnectedLevels():
    var levelNames = [];
    var map = get_node_or_null("World/Container/Map");
    if !map:
        return;
    for entity in map.get_children():
        if !entity is Entity:
            continue;
        if entity.identifier == "TRIGGER_CHANGELEVEL":
            levelNames.push_back(entity.getFields()["map"]);
    return levelNames;

func unload(clearCache = false):
    currentLevelNode.name+"_d"
    currentLevelNode.queue_free();
    currentLevelNode = null;
    if clearCache:
        currentLevelFileId = null;
        currentLevelMetadata = null;

func loadLevel(levelMetadata):
    currentLevelMetadata = levelMetadata;
    currentLevelFileId = levelMetadata.fileId;
    reloadLevel();
    var files = get_node("/root/App").files;
    camera.set_position(files[currentLevelFileId].GetLevelStart());
    camera.set_rotation(Vector3(0,0,0));
    
