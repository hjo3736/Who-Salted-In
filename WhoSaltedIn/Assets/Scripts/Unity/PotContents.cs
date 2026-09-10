using System.Collections.Generic;
using Game.Net;
using UnityEngine;

namespace Game.Unity
{
    public sealed class PotContents : MonoBehaviour
    {
        [SerializeField] MatchRuntime runtime;
        [SerializeField] Transform water;
        [SerializeField] Transform onion;
        [SerializeField] int waterStep = 3;
        [SerializeField] int onionStep = 1;

        void OnEnable()
        {
            if (runtime != null)
            {
                runtime.Looked += Apply;
            }
        }

        void OnDisable()
        {
            if (runtime != null)
            {
                runtime.Looked -= Apply;
            }
        }

        void Apply(IReadOnlyList<int> counts, IReadOnlyList<int> partial)
        {
            if (water != null && waterStep < counts.Count)
            {
                var poured = Mathf.Max(counts[waterStep], 0);

                water.gameObject.SetActive(poured > 0);

                if (poured > 0)
                {
                    var height = Mathf.Min(0.08f + poured * 0.12f, 0.34f);
                    water.localScale = new Vector3(0.6f, height, 0.6f);
                    water.localPosition = new Vector3(0f, -0.18f + height * 0.5f, 0f);
                }
            }

            if (onion != null && onionStep < counts.Count)
            {
                onion.gameObject.SetActive(Mathf.Max(counts[onionStep], 0) > 0);
            }
        }
    }
}
