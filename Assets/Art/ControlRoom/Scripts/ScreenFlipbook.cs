using UnityEngine;

// Minimal flipbook / image swap for a screen (URP Lit, image on _EmissionMap).
// Assign frames, or call SetImage() from a button. startOffset keeps screens of one group out of sync.
public class ScreenFlipbook : MonoBehaviour
{
    public Texture2D[] frames;
    public float fps = 3f;
    public int startOffset;
    public bool play = true;

    static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
    Renderer rend;
    MaterialPropertyBlock mpb;
    int last = -1;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (!play || frames == null || frames.Length == 0) return;
        int i = (startOffset + (int)(Time.time * fps)) % frames.Length;
        if (i != last) { Apply(frames[i]); last = i; }
    }

    public void SetImage(Texture2D tex) { play = false; Apply(tex); }

    void Apply(Texture2D tex)
    {
        rend.GetPropertyBlock(mpb);
        mpb.SetTexture(EmissionMap, tex);
        rend.SetPropertyBlock(mpb);
    }
}
