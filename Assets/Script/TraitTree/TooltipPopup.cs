using TMPro;
using UnityEngine;

namespace TraitTree
{
    /// <summary>
    /// 화면 도움말 팝업. 버튼 OnClick에 Show/Hide/Toggle을 연결해서 사용.
    ///
    /// 구조:
    ///   TooltipPopup (이 스크립트 부착, 항상 active)
    ///     └─ popupRoot (Show/Hide로 토글되는 자식)
    /// </summary>
    public class TooltipPopup : MonoBehaviour
    {
        [Tooltip("Show/Hide로 SetActive 토글할 대상. 보통 자식 'PopupContent'")]
        public GameObject popupRoot;

        [Tooltip("팝업 본문을 표시할 TMP 텍스트")]
        public TMP_Text bodyText;

        [TextArea(3, 8)]
        public string content =
            "악성코드 특성 트리 화면입니다.\n\n" +
            "노드를 클릭하면 특성 설명을 볼 수 있고,\n" +
            "더블클릭하면 코인을 소모해 특성을 해제할 수 있습니다.";

        public bool startHidden = true;

        void Start()
        {
            if (bodyText != null) bodyText.text = content;
            if (startHidden && popupRoot != null) popupRoot.SetActive(false);
        }

        public void Show()
        {
            if (bodyText != null) bodyText.text = content; // 내용 갱신
            if (popupRoot != null) popupRoot.SetActive(true);
        }

        public void Hide()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (popupRoot == null) return;
            if (popupRoot.activeSelf) Hide();
            else                       Show();
        }
    }
}
