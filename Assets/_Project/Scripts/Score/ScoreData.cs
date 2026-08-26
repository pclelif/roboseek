using System;
using Unity.Netcode;
using UnityEngine;

namespace Robot.Score
{
    [Serializable]
    public struct PlayerScoreData : INetworkSerializable, IEquatable<PlayerScoreData>
    {
        public ulong clientId;
        public string playerName;
        public int objectsFound;
        public int firstFinderCount;
        public int combatHits;
        public int accumulatedScore; // Previous rounds total

        public int FindScore => objectsFound * 100;
        public int FirstFinderScore => firstFinderCount * 25;
        public int CombatScore => combatHits * 10;
        public int RoundScore => FindScore + FirstFinderScore + CombatScore;
        public int TotalScore => accumulatedScore + RoundScore;

        public void ResetRound()
        {
            accumulatedScore += RoundScore;
            objectsFound = 0;
            firstFinderCount = 0;
            combatHits = 0;
        }

        public void ResetMatch()
        {
            accumulatedScore = 0;
            objectsFound = 0;
            firstFinderCount = 0;
            combatHits = 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref objectsFound);
            serializer.SerializeValue(ref firstFinderCount);
            serializer.SerializeValue(ref combatHits);
            serializer.SerializeValue(ref accumulatedScore);
        }

        public bool Equals(PlayerScoreData other)
        {
            return clientId == other.clientId &&
                   playerName == other.playerName &&
                   objectsFound == other.objectsFound &&
                   firstFinderCount == other.firstFinderCount &&
                   combatHits == other.combatHits &&
                   accumulatedScore == other.accumulatedScore;
        }
    }
}
