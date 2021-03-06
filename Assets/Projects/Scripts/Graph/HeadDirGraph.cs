﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace Graph
{
    /// <summary>
    /// 頭の向きを表すグラフ
    /// </summary>
    public class HeadDirGraph : MonoBehaviour
    {

        public List<HeadDirGraphSetting> graphSettingList;	//どのように頭の向きのデータをグラフに表示するかの設定
        public GraphDataSource InputData;					//頭の向きを持っている、IHeadDirDataを実装したクラス
        public BarChart Output_Bar;							//出力先のバーグラフ
        IHeadDirData input;
        List<Data> dataList;

        void Awake()
        {
            input = InputData.GetComponent<IHeadDirData>();
            InputData.OnGraphDataChange.Subscribe(_ =>
            {
                //グラフに表示するデータが変更された際に実行される
                //最新のデータを取得し、保持する
                dataList = input.GetHeadDirDatas();
                AttatchDataToGraph();
            });
        }

        /// <summary>
        /// 頭の向きを表すのに必要なデータ
        /// </summary>
        public class Data
        {
            Graph.Time time;			//検知した時間

            /// <summary>
            /// 頭の向き1
            /// </summary>
            SleepData.HeadDir HeadDir1;

            /// <summary>
            /// 頭の向き2
            /// </summary>
            SleepData.HeadDir HeadDir2;

            /// <summary>
            /// 頭の向き3
            /// </summary>
            SleepData.HeadDir HeadDir3;

            public Data(
                Graph.Time time,
                SleepData.HeadDir headDir1,
                SleepData.HeadDir headDir2,
                SleepData.HeadDir headDir3)
            {
                this.time = time;
                this.HeadDir1 = headDir1;
                this.HeadDir2 = headDir2;
                this.HeadDir3 = headDir3;
            }

            /// <summary>
            /// データの検知時間を取得します
            /// </summary>
            public Graph.Time GetTime()
            {
                return this.time;
            }

            /// <summary>
            /// 頭の向き1を取得します
            /// </summary>
            public SleepData.HeadDir GetHeadDir1()
            {
                return this.HeadDir1;
            }

            /// <summary>
            /// 頭の向き2を取得します
            /// </summary>
            public SleepData.HeadDir GetHeadDir2()
            {
                return this.HeadDir2;
            }

            /// <summary>
            /// 頭の向き3を取得します
            /// </summary>
            public SleepData.HeadDir GetHeadDir3()
            {
                return this.HeadDir3;
            }
        }

        [System.Serializable]
        /// <summary>
        /// 頭の向きのデータをグラフでどのように表示するかを設定するための項目をまとめたクラス
        /// </summary>
        public class HeadDirGraphSetting
        {
            [SerializeField]
            SleepData.HeadDir headDir;
            [SerializeField]
            LabelData.Label label;
            [SerializeField]
            float valueRate;

            /// <summary>
            /// どの向きか取得します
            /// </summary>
            /// <returns>The head dir.</returns>
            public SleepData.HeadDir GetHeadDir()
            {
                return this.headDir;
            }

            /// <summary>
            /// ラベルを取得します
            /// </summary>
            public LabelData.Label GetLabel()
            {
                return label;
            }

            /// <summary>
            /// ラベルの値の全体との比率を取得します
            /// バーチャートでの表示の高さに利用されます
            /// </summary>
            public float GetValueRate()
            {
                return valueRate;
            }
        }

        //データをグラフに設定します
        void AttatchDataToGraph()
        {
            if (dataList == null)
                return;
            //グラフに表示するためにラベルデータを作成
            List<LabelData> labelDataList = TransSensingDataToLabelData(dataList);
            //バーグラフに呼吸データを設定・表示
            SetHeadDirDataToBarChart(dataList, labelDataList);
        }

        /// <summary>
        /// 頭の向きグラフを表示する
        /// </summary>
        /// <param name="headDirDataList">頭の向きデータリスト</param>
        /// <param name="labelDataList">ラベルデータリスト</param>
        void SetHeadDirDataToBarChart(List<Data> headDirDataList, List<LabelData> labelDataList)
        {
            List<Vector2> xValueRangeList = new List<Vector2>();
            List<float> yValueList = new List<float>();

            for (int i = 0; i < headDirDataList.Count - 1; i++)
            {
                // 0~10, 10~20, 20~30秒のデータを順に詰める
                for (int j = 0; j < 3; j++)
                {
                    float xStart = Graph.Time.GetPositionRate(
                        headDirDataList[i].GetTime().Value.AddSeconds(j * 10),
                        headDirDataList.First().GetTime().Value,
                        headDirDataList.Last().GetTime().Value);
                    float xEnd = Graph.Time.GetPositionRate(
                        headDirDataList[i].GetTime().Value.AddSeconds(j * 10 + 10),
                        headDirDataList.First().GetTime().Value,
                        headDirDataList.Last().GetTime().Value);
                    float yValueRate;
                    if (j == 0) {
                        yValueRate = graphSettingList
                            .Where(
                                setting =>
                                    setting.GetHeadDir().Equals(
                                        headDirDataList[i].GetHeadDir1()))
                            .First().GetValueRate();
                    }
                    else if (j == 1)
                    {
                        yValueRate = graphSettingList
                            .Where(
                                setting =>
                                    setting.GetHeadDir().Equals(
                                        headDirDataList[i].GetHeadDir2()))
                            .First().GetValueRate();
                    }
                    else
                    {
                        yValueRate = graphSettingList
                            .Where(
                                setting =>
                                    setting.GetHeadDir().Equals(
                                        headDirDataList[i].GetHeadDir3()))
                            .First().GetValueRate();
                    }
                    xValueRangeList.Add(new Vector2(xStart, xEnd));
                    yValueList.Add(yValueRate);
                }
            }
            List<LabelData.Label> labelList = labelDataList.Select(
                labelData => labelData.GetLabel()).ToList();
            Output_Bar.SetData(xValueRangeList, yValueList, labelList);
        }

        //取得した頭の向きのデータをグラフに表示しやすいようにラベルデータへ変換する
        List<LabelData> TransSensingDataToLabelData(List<Data> dataList)
        {
            List<LabelData> labelDataList = new List<LabelData>();
            foreach (Data data in dataList)
            {
                labelDataList.Add(new LabelData(1f, MatchLabel(data, 0)));
                labelDataList.Add(new LabelData(1f, MatchLabel(data, 1)));
                labelDataList.Add(new LabelData(1f, MatchLabel(data, 2)));
            }
            return labelDataList;
        }

        /// <summary>
        /// 取得した頭の向きのデータから、インスペクターで設定したラベルを取得する
        /// </summary>
        /// <param name="data">頭の向きデータクラス</param>
        /// <returns>ラベルデータ</returns>
        LabelData.Label MatchLabel(Data data, int index)
        {
            foreach (HeadDirGraphSetting setting in graphSettingList)
            {
                switch (index)
                {
                    case 0:
                        if (setting.GetHeadDir().Equals(data.GetHeadDir1()))
                            return setting.GetLabel();
                        break;
                    case 1:
                        if (setting.GetHeadDir().Equals(data.GetHeadDir2()))
                            return setting.GetLabel();
                        break;
                    case 2:
                        if (setting.GetHeadDir().Equals(data.GetHeadDir3()))
                            return setting.GetLabel();
                        break;
                    default:
                        break;
                }
            }
            return null;
        }
    }

    public interface IHeadDirData
    {
        List<HeadDirGraph.Data> GetHeadDirDatas();
    }
}
