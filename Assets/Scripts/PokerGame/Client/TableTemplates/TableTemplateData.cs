////////////////////
//       RECK       //
////////////////////


using System;
using UnityEngine;


namespace WCC.Poker.Client
{
    [CreateAssetMenu(fileName = "TableTemplates", menuName = "WCC/Designs/Table")]
    public class TableTemplateData : ScriptableObject
    {
        public TableDesigns[] Templates;

        [Serializable]
        public class TableDesigns
        {
            public string TableName;
            public Sprite TableSprite;
        }
        //

    }
}
