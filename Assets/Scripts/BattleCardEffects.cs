using UnityEngine;

namespace Pupverse
{
    // Runtime-only line effects: never change the card or arena materials.
    [DisallowMultipleComponent]
    public sealed class BattleCardEffects : MonoBehaviour
    {
        [SerializeField] Shader effectShader = null;
        LineRenderer outline, energy;
        Material material;
        readonly Vector3[] points = new Vector3[33];

        public static Color StatColor(CardStat stat) => stat switch
        {
            CardStat.Power => new Color(1f, 0.4f, 0.15f),
            CardStat.Speed => new Color(0.15f, 1f, 1f),
            CardStat.Intelligence => new Color(0.7f, 0.4f, 1f),
            CardStat.Defence => new Color(0.3f, 1f, 0.5f),
            _ => new Color(1f, 0.85f, 0.25f)
        };

        public void Show(CardStat stat, Vector3 centre, Vector3 core, float progress, bool boosted)
        {
            if (!isActiveAndEnabled) return;
            if (outline == null && !CreateLines()) return;
            Color color = StatColor(stat);
            float radius = 0.7f + (GameSettings.ReducedMotion ? 0f : progress * 0.35f);
            outline.startColor = outline.endColor = color;
            outline.widthMultiplier = boosted ? 0.065f : 0.035f;
            outline.enabled = true;
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i / 32f * Mathf.PI * 2f;
                float r = radius;
                if (stat == CardStat.Luck) r *= 0.7f + 0.3f * Mathf.Cos(angle * 5f);
                if (stat == CardStat.Defence)
                {
                    float x = Mathf.Sin(angle);
                    points[i] = centre + new Vector3(x * radius, Mathf.Cos(angle) * radius * 1.3f, -0.15f);
                }
                else if (stat == CardStat.Intelligence || stat == CardStat.Luck)
                    points[i] = centre + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, -0.12f);
                else
                    points[i] = centre + new Vector3(Mathf.Cos(angle) * r, -0.8f, Mathf.Sin(angle) * r);
            }
            outline.SetPositions(points);
            energy.enabled = stat != CardStat.Defence;
            energy.startColor = color;
            energy.endColor = new Color(color.r, color.g, color.b, 0.2f);
            energy.widthMultiplier = boosted ? 0.09f : 0.045f;
            Vector3 end = stat == CardStat.Speed ? centre - Vector3.forward * 1.1f : core + Vector3.up * 0.5f;
            energy.SetPosition(0, centre);
            energy.SetPosition(1, Vector3.Lerp(centre, end, 0.5f) + Vector3.up * (stat == CardStat.Intelligence ? 0.4f : 0f));
            energy.SetPosition(2, end);
        }

        bool CreateLines()
        {
            Shader shader = effectShader != null ? effectShader : Shader.Find("Sprites/Default");
            if (shader == null) return false;
            material = new Material(shader) { hideFlags = HideFlags.DontSave };
            outline = CreateLine("Stat outline", 33);
            energy = CreateLine("Stat energy", 3);
            return true;
        }

        LineRenderer CreateLine(string label, int count)
        {
            var child = new GameObject(label) { hideFlags = HideFlags.DontSave };
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = count;
            line.numCapVertices = 4;
            line.enabled = false;
            return line;
        }

        public void Clear()
        {
            if (outline != null) outline.enabled = false;
            if (energy != null) energy.enabled = false;
        }

        void OnDisable() => Clear();
        void OnDestroy()
        {
            if (outline != null) Destroy(outline.gameObject);
            if (energy != null) Destroy(energy.gameObject);
            if (material != null) Destroy(material);
        }
    }
}
