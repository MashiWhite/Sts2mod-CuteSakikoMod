using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Nodes;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes
{
    public partial class ChordLibraryScreen : Control
    {
        
        private static TaskCompletionSource<List<string>> _tcsMultiple;
        private static List<string> _selectedChords;
        private static int _targetCount;

        private bool _isSelectMode;
        private bool _isCancelled;

        // ====== 公共入口 ======
        public static Task<List<string>> SelectChords(int count)
        {
            _tcsMultiple?.TrySetCanceled();
            _tcsMultiple = new TaskCompletionSource<List<string>>();
            _selectedChords = new List<string>();
            _targetCount = count;

            var screen = new ChordLibraryScreen();
            screen._isSelectMode = true;
            screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            // 不设置 MouseFilter，避免全屏拦截
            screen.MouseFilter = MouseFilterEnum.Ignore;
            NRun.Instance.GlobalUi.AddChild(screen);
            return _tcsMultiple.Task;
        }
        
        private static ChordLibraryScreen _instance;
        public static void OpenBrowse()
        {
            if (_instance != null && GodotObject.IsInstanceValid(_instance))
            {
                // 已经打开则忽略或带到前台（可选）
                return;
            }
            _instance = new ChordLibraryScreen();
            _instance._isSelectMode = false;
            _instance.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _instance.MouseFilter = MouseFilterEnum.Stop;
            NRun.Instance.GlobalUi.AddChild(_instance);
            _instance.TreeExited += () => _instance = null;
        }

        // ====== 界面构建 ======
        public override void _Ready()
        {
            // 将自身移到 GlobalUi 最底层，避免遮挡其它 UI
            var parent = GetParent();
            if (parent != null)
                parent.MoveChild(this, 0);

            // 半透明背景：从顶部 UI 下方开始（OffsetTop = 80）
            var bg = new ColorRect
            {
                Color = new Color(0, 0, 0, 0.6f),
                MouseFilter = MouseFilterEnum.Stop
            };
            bg.AnchorLeft = 0;
            bg.AnchorRight = 1;
            bg.AnchorTop = 0;
            bg.AnchorBottom = 1;
            bg.OffsetTop = 80;   // 顶部留出 80px 给血条等 UI
            bg.OffsetBottom = 200; // 底部留出空位
            bg.GuiInput += (InputEvent e) =>
            {
                if (!_isSelectMode && e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                    QueueFree();
            };
            AddChild(bg);

            // 顶部颜色条：定位在 y=80，高度 60
            var topBar = new ColorRect
            {
                Color = new Color(0.15f, 0.1f, 0.2f),
                MouseFilter = MouseFilterEnum.Ignore
            };
            topBar.AnchorLeft = 0;
            topBar.AnchorRight = 1;
            // 不使用 Top 锚点，改用固定位置
            topBar.Position = new Vector2(0, 80);
            topBar.Size = new Vector2(GetViewportRect().Size.X, 60);
            AddChild(topBar);

            var titleLabel = new Label
            {
                Text = _isSelectMode ? $"选择 {_targetCount} 个和弦 (已选 0)" : "和弦图鉴",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }; 
            // 让标题填满整个 topBar，实现居中
            titleLabel.AnchorLeft = 0;
            titleLabel.AnchorRight = 1;
            titleLabel.AnchorTop = 0;
            titleLabel.AnchorBottom = 1;
            titleLabel.Position = new Vector2(-930, 0);
            // 不再需要 Position
            titleLabel.AddThemeFontSizeOverride("font_size", 24);
            titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
            topBar.AddChild(titleLabel);
         

            // 滚动区域（您之前的手动偏移继续保留）
            var scroll = new ScrollContainer();
            scroll.AnchorLeft = 0;
            scroll.AnchorRight = 1;
            scroll.AnchorTop = 0;
            scroll.AnchorBottom = 1;
            scroll.OffsetLeft = 800;
            scroll.OffsetTop = 200;     // 您手动调节的值
            scroll.OffsetRight = -40;
            scroll.OffsetBottom = -20;
            AddChild(scroll);


            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 24);
            scroll.AddChild(vbox);

            var categories = new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant, ChordCategory.Anon, ChordCategory.Bonus };
            foreach (var cat in categories)
            {
                var chords = ChordManager.AllChords.Values.Where(c => c.Category == cat).ToList();
                if (chords.Count == 0) continue;

                var catLabel = new Label { Text = GetCategoryDisplayName(cat) };
                catLabel.AddThemeFontSizeOverride("font_size", 20);
                catLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
                vbox.AddChild(catLabel);

                var grid = new GridContainer { Columns = 6 };
                foreach (var chordDef in chords)
                {
                    var btn = new ChordButton();
                    btn.Setup(chordDef.Id, 1);

                    // 仅选择模式绑定交互
                    if (_isSelectMode)
                    {
                        btn.Pressed += () =>
                        {
                            if (_selectedChords.Count >= _targetCount) return;

                            if (_selectedChords.Contains(chordDef.Id))
                            {
                                _selectedChords.Remove(chordDef.Id);
                                btn.Modulate = Colors.White;
                            }
                            else
                            {
                                btn.Modulate = new Color(1, 1, 0.5f);
                                _selectedChords.Add(chordDef.Id);
                            }

                            titleLabel.Text = $"选择 {_targetCount} 个和弦 (已选 {_selectedChords.Count})";

                            if (_selectedChords.Count == _targetCount)
                            {
                                _tcsMultiple?.TrySetResult(_selectedChords.ToList());
                                QueueFree();
                            }
                        };
                    }

                    grid.AddChild(btn);
                }
                vbox.AddChild(grid);
            }
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            // 选择模式下仅允许 ESC 取消；浏览模式下 ESC 或右键关闭
            if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            {
                if (_isSelectMode)
                {
                    _isCancelled = true;
                    _tcsMultiple?.TrySetResult(new List<string>());
                }
                QueueFree();
                AcceptEvent();
            }
            if (!_isSelectMode && @event is InputEventMouseButton mouse
                && mouse.ButtonIndex == MouseButton.Right
                && mouse.Pressed)
            {
                QueueFree();
                AcceptEvent();
            }
        }

        public override void _ExitTree()
        {
            if (_isSelectMode && _tcsMultiple != null && !_tcsMultiple.Task.IsCompleted)
                _tcsMultiple.TrySetResult(new List<string>());
        }

        private static string GetCategoryDisplayName(ChordCategory cat) => cat switch
        {
            ChordCategory.Major => "大三和弦",
            ChordCategory.Minor => "小三和弦",
            ChordCategory.Dominant => "属七和弦",
            ChordCategory.Anon => "爱音和弦",
            ChordCategory.Bonus => "额外和弦",
            _ => "其他和弦"
        };
    }
}