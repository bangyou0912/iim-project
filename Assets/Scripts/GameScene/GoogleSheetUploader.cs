using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

/// <summary>
/// 用來把 CSV 內容上傳到 Google 試算表（透過 Google Apps Script Web App）
/// 用法：
/// GoogleSheetUploader.Instance.UploadCsv("玩家每回合統計", csvString);
/// </summary>
public class GoogleSheetUploader : MonoBehaviour
{
    public static GoogleSheetUploader Instance;

    [Header("Apps Script Web App URL")]
    [Tooltip("將你部署後的 Apps Script Web App URL 填在這裡（類似 https://script.google.com/macros/s/XXXXX/exec ）")]
    public string webAppUrl = "https://script.google.com/macros/s/AKfycbxB8nsFuye4jZ6B_Oihz39cpoDvZ3jx1yEO1bwx3uSBUeGspzGoPBGJhyC0uaC4AsWrdA/exec";

    [Header("Spreadsheet 設定（伺服端也可覆寫）")]
    [Tooltip("可留空，若留空則由 Apps Script 端使用預設常數 SPREADSHEET_ID")]
    public string spreadsheetId = "";

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    /// <summary>
    /// 上傳 CSV 到 Google Sheets。
    /// sheetBaseName：希望建立/使用的工作表分頁名稱（若已存在，伺服端會自動另開新分頁）。
    /// csvContent：你的 CSV 原文（含表頭、換行）。
    /// </summary>
    public void UploadCsv(string sheetBaseName, string csvContent)
    {
        StartCoroutine(CoUploadCsv(sheetBaseName, csvContent));
    }

    private IEnumerator CoUploadCsv(string sheetBaseName, string csvContent)
    {
        if (string.IsNullOrEmpty(webAppUrl))
        {
            Debug.LogError("[GoogleSheetUploader] webAppUrl 尚未設定。");
            yield break;
        }

        // 盡量統一 \n，避免不同平台換行差異
        string csvNorm = (csvContent ?? "").Replace("\r\n", "\n");

        // 改用表單避免 CORS 預檢
        WWWForm form = new WWWForm();
        form.AddField("sheetBaseName", string.IsNullOrEmpty(sheetBaseName) ? "Upload" : sheetBaseName);
        form.AddField("csv", csvNorm);
        if (!string.IsNullOrEmpty(spreadsheetId))
            form.AddField("spreadsheetId", spreadsheetId);

        using (UnityWebRequest req = UnityWebRequest.Post(webAppUrl, form))
        {
            // 小提醒：WWWForm 會自動把 Content-Type 設為 multipart/form-data；
            // 也能改用 application/x-www-form-urlencoded（更保險），如下兩行擇一：
            // var bytes = Encoding.UTF8.GetBytes($"sheetBaseName={UnityWebRequest.EscapeURL(sheetBaseName)}&csv={UnityWebRequest.EscapeURL(csvNorm)}&spreadsheetId={UnityWebRequest.EscapeURL(spreadsheetId)}");
            // var req = new UnityWebRequest(webAppUrl, "POST") { uploadHandler = new UploadHandlerRaw(bytes), downloadHandler = new DownloadHandlerBuffer() };
            // req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");

            req.downloadHandler = new DownloadHandlerBuffer();

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool isError = req.result != UnityWebRequest.Result.Success;
#else
        bool isError = req.isNetworkError || req.isHttpError;
#endif
            if (isError)
            {
                Debug.LogError($"[GoogleSheetUploader] 上傳失敗：{req.responseCode} {req.error}\n{req.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[GoogleSheetUploader] 上傳成功：{req.downloadHandler.text}");
            }
        }
    }


    [System.Serializable]
    private class UploadPayload
    {
        public string sheetBaseName;   // 想要的分頁基底名稱
        public string csv;             // CSV 內容
        public string spreadsheetId;   // 可選，若空則使用伺服端常數
    }
}
