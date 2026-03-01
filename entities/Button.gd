extends Trigger
class_name EntButton

var wait;

var active : bool;
var wait_timer : float;
var btn_material : Material;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
    wait_timer = 0;
    active = false;

func configure(target_ : String, wait_ : float):
    super.bind(true, target_, false);
    wait = wait_;
    # Bind button material
    var mesh = get_node_or_null("Mesh");
    if mesh:
        for surface in mesh.mesh.get_surface_count():
            var mat = mesh.mesh.surface_get_material(surface);
            var texName = mat.albedo_texture.resource_name;
            if texName.begins_with("+a") || texName.begins_with("+0"):
                btn_material = mat;

func reset():
    active = false;
    if btn_material:
        var texName = btn_material.albedo_texture.resource_name;
        var resetTex = get_node("/root/App").view3D.currentLevelNode.getTexture(texName.replace("+0", "+a"));
        btn_material.albedo_texture = resetTex;

func activate():
    super.trigger(self);
    active = true;
    if btn_material:
        var texName = btn_material.albedo_texture.resource_name;
        var resetTex = get_node("/root/App").view3D.currentLevelNode.getTexture(texName.replace("+a", "+0"));
        btn_material.albedo_texture = resetTex;

func _process(delta : float) -> void:
    if active:
        wait_timer += delta;
        if wait_timer >= wait:
            wait_timer = 0.0;
            reset();
        
