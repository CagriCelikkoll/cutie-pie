using System;
using System.Collections.Generic;
using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>
    /// Bir özel sipariş pastasının içeriği. Hem fişteki hedef pastayı hem oyuncunun yaptığı pastayı taşır.
    /// Kayda (JsonUtility) yazılabilmesi için yalnızca alan ve liste kullanır.
    /// </summary>
    [Serializable]
    public class CakeBuild
    {
        /// <summary>Taban parçasının CakePartData.id değeri.</summary>
        public string baseId;

        /// <summary>Krema parçasının CakePartData.id değeri.</summary>
        public string creamId;

        /// <summary>Pastanın üstündeki süsler.</summary>
        public List<DecorationPlacement> decorations = new List<DecorationPlacement>();

        /// <summary>Kutu parçasının CakePartData.id değeri.</summary>
        public string boxId;
    }

    /// <summary>Pastanın üstüne konmuş tek bir süs.</summary>
    [Serializable]
    public class DecorationPlacement
    {
        /// <summary>Süs parçasının CakePartData.id değeri.</summary>
        public string decorationId;

        /// <summary>Pasta üstündeki konum; her eksen 0–1 arası (sol alt 0,0; sağ üst 1,1).</summary>
        public Vector2 position;
    }
}
