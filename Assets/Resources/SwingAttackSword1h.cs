using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SwingAttackSword1h", menuName = "Scriptable Objects/SwingAttackSword1h")]
public class SwingAttackSword1h : ScriptableObject
{
    [Serializable]
    public struct SwingSample
    {
        public float time;              // нормализованное время (0..1)
        public Vector3 bladeBase;       // позиция основания клинка (относительно корня персонажа)
        public Vector3 bladeTip;        // позиция кончика клинка
        public Quaternion bladeRotation;// поворот клинка (опционально)
    }
    
    public float duration;              // длительность анимации в секундах
    public SwingSample[] samples;       // массив сэмплов (например, 30-60 штук)
    
    public (Vector3 bladeBase, Vector3 bladeTip) SampleAt(float normalizedTime)
    {
        // Находим два ближайших сэмпла и интерполируем
        normalizedTime = Mathf.Clamp01(normalizedTime);
        float idx = normalizedTime * (samples.Length - 1);
        int i0 = Mathf.FloorToInt(idx);
        int i1 = Mathf.Min(i0 + 1, samples.Length - 1);
        float t = idx - i0;
        
        return (
            Vector3.Lerp(samples[i0].bladeBase, samples[i1].bladeBase, t),
            Vector3.Lerp(samples[i0].bladeTip, samples[i1].bladeTip, t)
        );
    }
}
