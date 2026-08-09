using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class CreateClanView : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private TMP_InputField _descriptionInputField;
    [SerializeField] private Button _createButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TMP_Text _errorText;
    
    private const int MAX_NAME_LENGTH = 20;
    private const string FORBIDDEN_CHARS = "<>\"'&/\\{}[]|*?%$#@!`";

    private UIModalAnimator _modalAnimator;
    
    public event Action<string, string> OnCreateClicked;
    public event Action OnCancelClicked;
    
    private void Awake()
    {
        _modalAnimator = GetComponent<UIModalAnimator>();
        if (_modalAnimator == null)
            _modalAnimator = gameObject.AddComponent<UIModalAnimator>();

        if (_createButton != null)
            _createButton.onClick.AddListener(HandleCreateClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(() => OnCancelClicked?.Invoke());
        
        if (_nameInputField != null)
            _nameInputField.onValueChanged.AddListener(ValidateInput);

        if (_descriptionInputField != null)
            _descriptionInputField.onValueChanged.AddListener(ValidateInput);
        
        if (_createButton != null)
            _createButton.interactable = false;

        ClearError();
    }
    
    private void OnDestroy()
    {
        if (_createButton != null)
            _createButton.onClick.RemoveAllListeners();

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveAllListeners();
        
        if (_nameInputField != null)
            _nameInputField.onValueChanged.RemoveAllListeners();

        if (_descriptionInputField != null)
            _descriptionInputField.onValueChanged.RemoveAllListeners();
    }
    
    public void Reset()
    {
        if (_nameInputField != null)
            _nameInputField.text = "";

        if (_descriptionInputField != null)
            _descriptionInputField.text = "";

        ClearError();

        if (_createButton != null)
            _createButton.interactable = false;
    }
    
    private void ValidateInput(string value)
    {
        bool isValid = ValidateClanName(_nameInputField?.text ?? "");
        if (_createButton != null)
            _createButton.interactable = isValid;
        
        if (!isValid && !string.IsNullOrEmpty(_nameInputField?.text))
            ShowError("Название клана должно быть от 1 до 20 символов и не содержать запрещенные символы");
        else
            ClearError();
    }
    
    private bool ValidateClanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        
        if (name.Length > MAX_NAME_LENGTH)
            return false;
        
        foreach (char c in FORBIDDEN_CHARS)
        {
            if (name.Contains(c))
                return false;
        }
        
        return true;
    }
    
    private void HandleCreateClicked()
    {
        string name = _nameInputField?.text?.Trim() ?? "";
        string description = _descriptionInputField?.text?.Trim() ?? "";
        
        if (ValidateClanName(name))
            OnCreateClicked?.Invoke(name, description);
        else
            ShowError("Пожалуйста, введите корректное название клана");
    }
    
    public void ShowError(string errorMessage)
    {
        if (_errorText != null)
        {
            _errorText.text = errorMessage;
            _errorText.gameObject.SetActive(true);
        }
    }
    
    public void ClearError()
    {
        if (_errorText != null)
        {
            _errorText.text = "";
            _errorText.gameObject.SetActive(false);
        }
    }
    
    public void ShowValidationError(string field, string[] errors)
    {
        if (errors != null && errors.Length > 0)
        {
            string errorMessage = string.Join("\n", errors);
            ShowError($"{field}: {errorMessage}");
        }
    }
    
    public void Close()
    {
        if (_modalAnimator != null && _modalAnimator.IsClosing)
            return;

        if (_modalAnimator != null)
        {
            _modalAnimator.PlayHide(() => Destroy(gameObject));
            return;
        }

        Destroy(gameObject);
    }
    
    public class Factory : PlaceholderFactory<CreateClanView> { }
}
