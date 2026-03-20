using PurrNet.Prediction;
using UnityEngine;

namespace Game.Scripts
{
    public class PredictedCombat : PredictedIdentity<PredictedCombat.Input, PredictedCombat.State>
    {
        [SerializeField] private SwingAttackSword1h backedAttackSamples;
        
        protected override void Simulate(Input input, ref State state, float delta)
        {
            
        }
        
        public struct Input : IPredictedData
        {
            public void Dispose()
            {}
        }

        public struct State : IPredictedData<State>
        {
            public void Dispose()
            {}
        }
    }
}