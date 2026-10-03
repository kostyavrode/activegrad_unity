using UnityEngine;

[CreateAssetMenu(fileName = "JumpGameConfig", menuName = "MiniGames/Jump Game Config")]
public class JumpGameConfig : ScriptableObject
{
    [Header("Player")]
    public Sprite playerSprite;
    public Color  playerColor = new Color(0.35f, 0.75f, 1f);

    [Header("Rock obstacle")]
    public Sprite rockSprite;
    public Color  rockColor   = new Color(0.55f, 0.32f, 0.18f);

    [Header("Beam obstacle")]
    public Sprite beamSprite;
    public Color  beamColor   = new Color(0.70f, 0.25f, 0.25f);

    [Header("Coin")]
    public Sprite coinSprite;
    public Color  coinColor   = new Color(1f, 0.85f, 0.1f);

    [Header("Background")]
    public Color skyColor     = new Color(0.804f, 0.847f, 0.769f);
    public Color groundColor  = new Color(0.42f, 0.62f, 0.45f);
    public Sprite bgHillSprite;
    public Color bgLayer0Color = new Color(0.74f, 0.82f, 0.74f);
    public Color bgLayer1Color = new Color(0.67f, 0.78f, 0.68f);
    public Color bgLayer2Color = new Color(0.60f, 0.74f, 0.61f);
}
