using System;
using UnityEngine;
using Debug = DebugEx;

public class PageController : MonoBehaviour
{
    public static PageController Instance;

    // 내가 사용할 페이지들만 할당해주면됨
    [SerializeField] public GameObject[] pages;
    
    private InputManager _inputManager;
    [SerializeField] private VideoManager videoManager;
    
    private void Start()
    {
        GoTitle();
    }

    // 유효한 페이지만 유지하도록 함
    private int _currentPage;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (pages.Length == 0) return;

            // 현재페이지 한번 더 열기 방지
            if (_currentPage == value) return;
            
            // 직전에 열려있던 페이지 저장
            _pastPage = _currentPage;
            
            // 다음페이지 열때, 페이지 범위 초과시 0페이지
            if (value >= pages.Length)
            {
                _currentPage = value % pages.Length;
            }
            // 음수 페이지 열면, 대총 범위 맞게 재설정
            else if (value < 0)
            {
                _currentPage = value + pages.Length;
            }
            else
            {
                _currentPage = value;
            }
        }
    }

    private int _pastPage;
    
    void Awake()
    {
        Instance = this;
    }

    // 페이지가 늦게 닫혀야할 때를 위한 페이지 닫기
    public void CloseSinglePage(int x)
    {
        Debug.Log("Close page index : " + x);
        pages[x].SetActive(false);
    }

    // 다음페이지 열기
    public void LoadNextPage()
    {
        ++CurrentPage;
        OpenPage(CurrentPage);             
        CloseSinglePage(_pastPage);  
    }
    
    public void GoTitle()
    {
        OpenPage(0);

        for (int i = 1; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
        }
    }
    
    public void OpenPage(int pageNum)
    {
        // 페이지를 열었을 때, inputManager의 page와 index 갱신
        InputManager.Instance.SetCurrentIndex(pageNum,0);
        Debug.Log("OpenPage : " + pageNum);

        if (pageNum < pages.Length)
        {
            pages[pageNum].SetActive(true);
            CurrentPage = pageNum;
        }
    }
    
    public void OpenAndClosePage(int pageNum)
    {
        OpenPage(pageNum);
        CloseSinglePage(_pastPage);  
    }
}
