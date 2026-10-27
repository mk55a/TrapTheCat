using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.UI
{
    public sealed class ScreenNavigator : MonoBehaviour
    {
        [Tooltip("Where the screens are created; usually the Canvas itself.")]
        [SerializeField] private RectTransform _screenParent;
        [SerializeField] private List<UIScreen> _screenPrefabs = new List<UIScreen>();
        [SerializeField] private ConfirmationPopup _popupPrefab;

        private readonly List<UIScreen> _screens = new List<UIScreen>();
        private UIScreen _current;

        public ConfirmationPopup Popup { get; private set; }

        public void CreateScreens()
        {
            foreach (UIScreen prefab in _screenPrefabs)
            {
                UIScreen screen = Instantiate(prefab, _screenParent, false);
                screen.name = prefab.name;
                screen.gameObject.SetActive(false);
                _screens.Add(screen);
            }

            Popup = Instantiate(_popupPrefab, _screenParent, false);
            Popup.name = _popupPrefab.name;
            Popup.Hide();
        }

        public TScreen Get<TScreen>() where TScreen : UIScreen
        {
            foreach (UIScreen screen in _screens)
            {
                TScreen match = screen as TScreen;
                if (match != null)
                    return match;
            }

            Debug.LogError($"ScreenNavigator has no {typeof(TScreen).Name} prefab in its list.", this);
            return null;
        }

        public TScreen Open<TScreen>() where TScreen : UIScreen
        {
            TScreen screen = Get<TScreen>();
            if (_current != null && _current != screen)
                _current.Close();

            Popup.Hide();
            _current = screen;
            screen.Open();
            return screen;
        }
    }
}
