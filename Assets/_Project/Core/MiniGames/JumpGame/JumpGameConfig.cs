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
    public Color skyColor     = new Color(0.06f, 0.07f, 0.12f);
    public Color groundColor  = new Color(0.20f, 0.55f, 0.25f);
    public Sprite bgHillSprite;
    public Color bgLayer0Color = new Color(0.12f, 0.14f, 0.22f);
    public Color bgLayer1Color = new Color(0.10f, 0.18f, 0.18f);
    public Color bgLayer2Color = new Color(0.08f, 0.22f, 0.14f);
}
