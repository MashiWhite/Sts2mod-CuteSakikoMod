extends Panel

signal confirmed(value)
signal canceled

@onready var spin_box: SpinBox = $VBoxContainer/SpinBox
@onready var confirm_button: Button = $VBoxContainer/HBoxContainer/ConfirmButton
@onready var cancel_button: Button = $VBoxContainer/HBoxContainer/CancelButton

func _ready():
	confirm_button.pressed.connect(_on_confirm)
	cancel_button.pressed.connect(_on_cancel)

func _on_confirm():
	confirmed.emit(int(spin_box.value))
	queue_free() # 关闭弹窗

func _on_cancel():
	canceled.emit()
	queue_free() # 关闭弹窗
