using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class GoogleSheetUploader : MonoBehaviour
{
    string sheetURL = "https://script.google.com/macros/s/AKfycbys_1vx9qOWH_A39nCx-NUvp83LOing8vP5qV3XTSKn098WofO31KhgyuCds2QK0olFCg/exec";

    public void UploadPlayerData(
        string playerName, int actorNumber, int turnIndex, float timeUsed,
        int hintClickCount, bool discarded, int failCount, int gemSpent,
        int gemReward, int finalGems)
    {
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "playerName", playerName },
            { "actorNumber", actorNumber },
            { "turnIndex", turnIndex },
            { "timeUsed", timeUsed },
            { "hintClickCount", hintClickCount },
            { "discarded", discarded ? "是" : "否" },
            { "failCount", failCount },
            { "gemSpent", gemSpent },
            { "gemReward", gemReward },
            { "finalGems", finalGems }
        };

        StartCoroutine(PostData(sheetURL, data));
    }

    IEnumerator PostData(string url, Dictionary<string, object> data)
    {
        string json = JsonUtility.ToJson(new SerializationHelper(data));
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("成功上傳: " + request.downloadHandler.text);
        else
            Debug.LogError("上傳失敗: " + request.error);
    }

    // Helper class to serialize Dictionary
    [System.Serializable]
    public class SerializationHelper
    {
        public string playerName;
        public int actorNumber;
        public int turnIndex;
        public float timeUsed;
        public int hintClickCount;
        public string discarded;
        public int failCount;
        public int gemSpent;
        public int gemReward;
        public int finalGems;

        public SerializationHelper(Dictionary<string, object> dict)
        {
            playerName = dict["playerName"].ToString();
            actorNumber = (int)dict["actorNumber"];
            turnIndex = (int)dict["turnIndex"];
            timeUsed = (float)dict["timeUsed"];
            hintClickCount = (int)dict["hintClickCount"];
            discarded = dict["discarded"].ToString();
            failCount = (int)dict["failCount"];
            gemSpent = (int)dict["gemSpent"];
            gemReward = (int)dict["gemReward"];
            finalGems = (int)dict["finalGems"];
        }
    }
}
