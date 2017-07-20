using System;
using System.Collections.Generic;
using GeneratedDatasets;

namespace CnfBattleSys
{
    /// <summary>
    /// A batched set of animation/audio/fx events to execute.
    /// </summary>
    public class EventBlock
    {
        /// <summary>
        /// A single priority layer of the event block.
        /// </summary>
        public class Layer
        {
            public readonly AnimEvent[] animEvents;
            public readonly AudioEvent[] audioEvents;
            public readonly FXEvent[] fxEvents;

            public Layer (AnimEvent[] _animEvents, AudioEvent[] _audioEvents, FXEvent[] _fxEvents)
            {
                animEvents = _animEvents;
                audioEvents = _audioEvents;
                fxEvents = _fxEvents;
            }
        }

        /// <summary>
        /// The BattleCameraScript type attached to this event block.
        /// If not null, this is given to the camera harness when
        /// the event block is dispatched.
        /// </summary>
        public readonly BattleCameraScriptType battleCameraScript;

        /// <summary>
        /// The priority layers in this event block.
        /// These are sorted by priority. Layer 0 executes
        /// before layer 1 executes before layer 2, etc.
        /// </summary>
        public readonly Layer[] layers;
        
        public EventBlock (AnimEvent[] animEvents, AudioEvent[] audioEvents, FXEvent[] fxEvents, BattleCameraScriptType _battleCameraScript)
        {
            battleCameraScript = _battleCameraScript;
            int longestLength = animEvents.Length;
            if (audioEvents.Length > longestLength) longestLength = audioEvents.Length;
            if (fxEvents.Length > longestLength) longestLength = fxEvents.Length;
            List<AnimEvent> animEventsList = new List<AnimEvent>(longestLength);
            List<AudioEvent> audioEventsList = new List<AudioEvent>(longestLength);
            List<FXEvent> fxEventsList = new List<FXEvent>(longestLength);
            List<Layer> layersList = new List<Layer>(longestLength);
            Func<int, Layer> GetLayerAtPriority = (priority) =>
            {
                animEventsList.Clear();
                audioEventsList.Clear();
                fxEventsList.Clear();         
                for (int i = 0; i < longestLength; i++)
                {
                    if (i < animEvents.Length && animEvents[i].priority == priority) animEventsList.Add(animEvents[i]);
                    if (i < audioEvents.Length && audioEvents[i].priority == priority) audioEventsList.Add(audioEvents[i]);
                    if (i < fxEvents.Length && fxEvents[i].priority == priority) fxEventsList.Add(fxEvents[i]);
                }
                return new Layer(animEventsList.ToArray(), audioEventsList.ToArray(), fxEventsList.ToArray());
            };
            int priorityCeiling = int.MaxValue;
            while (true)
            {
                int highestPriority = int.MinValue;
                for (int i = 0; i < longestLength; i++)
                {
                    if (i < animEvents.Length && animEvents[i].priority < priorityCeiling && animEvents[i].priority > highestPriority)
                        highestPriority = animEvents[i].priority;
                    if (i < audioEvents.Length && audioEvents[i].priority < priorityCeiling && audioEvents[i].priority > highestPriority)
                        highestPriority = audioEvents[i].priority;
                    if (i < fxEvents.Length && fxEvents[i].priority < priorityCeiling && fxEvents[i].priority > highestPriority)
                        highestPriority = fxEvents[i].priority;
                }
                if (priorityCeiling != highestPriority)
                {
                    layersList.Add(GetLayerAtPriority(highestPriority));
                    priorityCeiling = highestPriority;
                }
                else break;
            }
            layers = layersList.ToArray();
        }
    }
}