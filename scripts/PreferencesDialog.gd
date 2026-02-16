extends Window


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
    get_node("Layout/GamePath/BrowseButton").connect("pressed", Callable(self, "browseGamePath"));
    connect("close_requested", Callable(self, "cancel"));
    get_node("Layout/Buttons/ApplyButton").connect("pressed", Callable(self, "apply"));
    get_node("Layout/Buttons/CancelButton").connect("pressed", Callable(self, "cancel"));

func setDefaults():
    get_node("Layout/GamePath/LineEdit").set_text(get_node("/root/App").gamePath);

func browseGamePath():
    var folderDialog = get_node("/root/App/FolderDialog");
    folderDialog.visible = true;
    await folderDialog.confirmed;
    get_node("Layout/GamePath/LineEdit").set_text(folderDialog.current_dir);
    
func cancel():
    setDefaults();
    visible = false;

func apply():
    get_node("/root/App").gamePath = get_node("Layout/GamePath/LineEdit").text;
