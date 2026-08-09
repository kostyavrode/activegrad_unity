public interface IWindow
{
    void Show();

    void Hide();

    void PushToBackground();

    void PopFromBackground();

    bool IsVisible { get; }
}
