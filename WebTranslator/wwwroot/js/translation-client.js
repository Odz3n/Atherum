(function () {
    'use strict';

    const CONFIG = {
        debounceDelay: 1000,
        minTextLength: 2,
        maxHistoryItems: 50
    };

    const elements = {
        sourceText: document.getElementById('sourceText'),
        sourceCount: document.getElementById('sourceCount'),
        translatedText: document.getElementById('translatedText'),
        translationStatus: document.getElementById('translationStatus'),
        translationInfo: document.getElementById('translationInfo'),
        translationInfoFooter: document.getElementById('translationInfoFooter'),
        fromLanguage: document.getElementById('fromLanguage'),
        clearButton: document.getElementById('clearButton'),
        copyButton: document.getElementById('copyButton'),
        statusDot: document.querySelector('.status-dot'),
        translationDot: document.querySelector('.translation-dot'),
        progressContainer: document.getElementById('progressContainer'),
        progressBar: document.getElementById('progressBar'),
        connectionStatus: document.getElementById('connectionStatus'),
        sessionId: document.getElementById('sessionId'),
        languageOptions: document.getElementById('languageOptions'),
        languageDropdownButton: document.getElementById('languageDropdownButton'),
        selectedLanguagesText: document.getElementById('selectedLanguagesText'),
        historyList: document.getElementById('historyList'),
        clearHistoryBtn: document.getElementById('clearHistoryBtn')
    };

    const state = {
        isTranslating: false,
        currentText: '',
        lastTranslation: '',
        detectedLanguage: null,
        progress: 0,
        isConnected: false
    };

    const MessageType = {
        Idle: 0,
        Empty: 1,
        ReadyForInput: 2,
        AwaitingInput: 3,
        Typing: 4,
        Clearing: 5,
        Valid: 6,
        ValidationError: 7,
        NullError: 8,
        EmptyError: 9,
        TooLong: 10,
        TooShort: 11,
        InvalidCharacters: 12,
        Configuring: 13,
        DetectingLanguage: 14,
        LanguageDetected: 15,
        Translating: 16,
        Translated: 17,
        TranslationPartial: 18,
        TranslationFailed: 19,
        NetworkError: 20,
        TimeoutError: 21,
        ApiError: 22,
        ServerError: 23,
        Info: 24,
        Warning: 25,
        Success: 26
    };

    const MessageTypeNames = {
        0: 'Idle',
        1: 'Empty',
        2: 'ReadyForInput',
        3: 'AwaitingInput',
        4: 'Typing',
        5: 'Clearing',
        6: 'Valid',
        7: 'ValidationError',
        8: 'NullError',
        9: 'EmptyError',
        10: 'TooLong',
        11: 'TooShort',
        12: 'InvalidCharacters',
        13: 'Configuring',
        14: 'DetectingLanguage',
        15: 'LanguageDetected',
        16: 'Translating',
        17: 'Translated',
        18: 'TranslationPartial',
        19: 'TranslationFailed',
        20: 'NetworkError',
        21: 'TimeoutError',
        22: 'ApiError',
        23: 'ServerError',
        24: 'Info',
        25: 'Warning',
        26: 'Success'
    };

    function getLanguageName(code) {
        const languages = {
            en: "English",
            ru: "Russian",
            uk: "Ukrainian",
            bg: "Bulgarian",
            de: "German",
            fr: "French",
            es: "Spanish"
        };
        return languages[code] || code;
    }

    function getSelectedLanguages() {
        const checkboxes = document.querySelectorAll("#languageOptions input[type='checkbox']");
        return [...checkboxes]
            .filter(x => x.checked)
            .map(x => x.value);
    }

    function isSignalRConnected() {
        return window.signalRClient?.isConnectedState() || false;
    }

    function updateCharCount() {
        if (!elements.sourceText || !elements.sourceCount) return;
        const count = elements.sourceText.value.length;
        elements.sourceCount.textContent = `${count} characters`;
    }

    function updateStatus(status, message, isError = false) {
        if (!elements.translationStatus) return;

        elements.translationStatus.textContent = message;
        elements.translationStatus.className = 'translation-status';

        if (isError) {
            elements.translationStatus.classList.add('status-error');
        } else {
            const statusMap = {
                Idle: 'status-idle',
                ReadyForInput: 'status-idle',
                AwaitingInput: 'status-idle',
                Empty: 'status-idle',
                Typing: 'status-processing',
                Valid: 'status-info',
                DetectingLanguage: 'status-processing',
                Translating: 'status-processing',
                Translated: 'status-success',
                Success: 'status-success',
                Info: 'status-info',
                Warning: 'status-warning',
                Error: 'status-error',
                TranslationFailed: 'status-error',
                NetworkError: 'status-error',
                TimeoutError: 'status-error',
                ApiError: 'status-error',
                ServerError: 'status-error',
                ValidationError: 'status-error',
                NullError: 'status-error',
                EmptyError: 'status-error',
                TooLong: 'status-error',
                TooShort: 'status-warning',
                InvalidCharacters: 'status-error',
                Clearing: 'status-idle'
            };
            elements.translationStatus.classList.add(statusMap[status] || 'status-idle');
        }

        updateStatusDot(status);
    }

    function updateStatusDot(status) {
        const statusMap = {
            Idle: 'status-idle',
            ReadyForInput: 'status-idle',
            AwaitingInput: 'status-idle',
            Empty: 'status-idle',
            Typing: 'status-processing',
            Valid: 'status-info',
            DetectingLanguage: 'status-processing',
            Translating: 'status-processing',
            Translated: 'status-success',
            Success: 'status-success',
            Info: 'status-info',
            Warning: 'status-warning',
            Error: 'status-error',
            TranslationFailed: 'status-error',
            NetworkError: 'status-error',
            TimeoutError: 'status-error',
            ApiError: 'status-error',
            ServerError: 'status-error',
            ValidationError: 'status-error',
            NullError: 'status-error',
            EmptyError: 'status-error',
            TooLong: 'status-error',
            TooShort: 'status-warning',
            InvalidCharacters: 'status-error',
            Clearing: 'status-idle'
        };

        const className = statusMap[status] || 'status-idle';

        if (elements.statusDot) {
            elements.statusDot.className = 'status-dot';
            elements.statusDot.classList.add(className);
        }

        if (elements.translationDot) {
            elements.translationDot.className = 'status-dot translation-dot';
            elements.translationDot.classList.add(className);
        }
    }

    function updateTranslationInfo(detectedLanguage, targetLanguages, fromLanguage) {
        let info = '';

        const fromLang = fromLanguage || elements.fromLanguage?.value || 'auto';
        const isAutoDetected = fromLang === 'auto' || fromLang === 'Auto detect';

        if (detectedLanguage && detectedLanguage !== 'auto') {
            if (isAutoDetected) {
                info += `Detected: ${getLanguageName(detectedLanguage)}`;
            } else {
                info += `From: ${getLanguageName(fromLang)}`;
            }
        } else if (fromLang && fromLang !== 'auto') {
            info += `From: ${getLanguageName(fromLang)}`;
        }

        if (targetLanguages && targetLanguages.length > 0) {
            if (info) info += ' → ';
            info += targetLanguages.map(l => getLanguageName(l)).join(', ');
        }

        if (!info) {
            info = 'Select a target language';
        }

        if (elements.translationInfo) {
            elements.translationInfo.textContent = info;
        }

        if (elements.translationInfoFooter) {
            elements.translationInfoFooter.textContent = info;
        }
    }

    function updateProgress(progress) {
        state.progress = progress || 0;

        if (elements.progressContainer) {
            if (progress > 0 && progress < 100) {
                elements.progressContainer.classList.add('active');
            } else {
                elements.progressContainer.classList.remove('active');
            }
        }

        if (elements.progressBar) {
            elements.progressBar.style.width = `${progress}%`;
        }
    }

    function updateConnectionStatus(connected, message) {
        state.isConnected = connected;

        if (!elements.connectionStatus) return;

        elements.connectionStatus.className = 'connection-status';

        if (connected) {
            elements.connectionStatus.classList.add('connected');
            elements.connectionStatus.innerHTML = `<span class="dot connected"></span> ${message || 'Connected'}`;
        } else {
            elements.connectionStatus.classList.add('disconnected');
            elements.connectionStatus.innerHTML = `<span class="dot disconnected"></span> ${message || 'Disconnected'}`;
        }
    }

    function showError(message) {
        updateStatus('Error', `❌ ${message}`, true);
    }

    function setTranslationText(text) {
        if (!elements.translatedText) return;
        elements.translatedText.value = text || '';
        elements.translatedText.classList.remove('empty');
        if (!text) {
            elements.translatedText.classList.add('empty');
        }
        state.lastTranslation = text || '';
    }

    function handleTranslationStatus(data) {
        if (!data) return;

        const type = typeof data.type === 'number' ? MessageTypeNames[data.type] : data.type;

        if (data.detectedLanguage) {
            state.detectedLanguage = data.detectedLanguage;
        }

        switch (type) {
            case 'Idle':
            case 'ReadyForInput':
            case 'AwaitingInput':
                updateStatus('Idle', '📝 Ready for input', false);
                if (data.isClearing) {
                    setTranslationText('');
                    updateTranslationInfo(null, null);
                }
                updateProgress(0);
                break;

            case 'Clearing':
                updateStatus('Clearing', '🧹 Clearing...', false);
                setTranslationText('');
                updateTranslationInfo(null, null);
                updateProgress(0);
                break;

            case 'Empty':
            case 'EmptyError':
                updateStatus('Idle', '📝 Type something to translate', false);
                updateProgress(0);
                break;

            case 'Typing':
                updateStatus('Typing', '✍️ Typing...', false);
                break;

            case 'Valid':
                updateStatus('Valid', '✅ Text validated', false);
                break;

            case 'TooShort':
                updateStatus('TooShort', '📏 Please enter at least 2 characters', false);
                break;

            case 'TooLong':
                updateStatus('TooLong', `📏 ${data.message}`, true);
                break;

            case 'InvalidCharacters':
                updateStatus('InvalidCharacters', `⚠️ ${data.message}`, true);
                break;

            case 'ValidationError':
            case 'NullError':
                updateStatus('ValidationError', `⚠️ ${data.message}`, true);
                break;

            case 'Configuring':
                updateStatus('Info', '⚙️ Configuring...', false);
                break;

            case 'DetectingLanguage':
                updateStatus('DetectingLanguage', '🔍 Detecting language...', false);
                updateProgress(20);
                break;

            case 'LanguageDetected': {
                const langName = getLanguageName(data.detectedLanguage);
                const score = data.detectedLanguageScore
                    ? ` (${Math.round(data.detectedLanguageScore * 100)}%)`
                    : '';
                updateStatus('Info', `🌐 Detected: ${langName}${score}`, false);
                updateTranslationInfo(
                    data.detectedLanguage,
                    getSelectedLanguages(),
                    elements.fromLanguage?.value || 'auto'
                );
                updateProgress(50);
                break;
            }

            case 'Translating':
                updateStatus('Translating', '🔄 Translating...', false);
                updateProgress(70);
                break;

            case 'Translated':
            case 'TranslationPartial':
                if (data.translatedText) {
                    setTranslationText(data.translatedText);
                }

                if (data.allTranslations && data.allTranslations.length > 1) {
                    const allTranslations = data.allTranslations
                        .map(t => `${getLanguageName(t.toLanguage)}: ${t.text}`)
                        .join('\n\n');
                    setTranslationText(allTranslations);
                }

                const fromLang = elements.fromLanguage?.value || 'auto';
                const originalText = data.originalText || state.currentText;

                if (data.allTranslations && data.allTranslations.length > 0) {
                    data.allTranslations.forEach(t => {
                        if (t.text && originalText) {
                            addHistoryItem(
                                originalText,
                                fromLang,
                                t.toLanguage,
                                t.text
                            );
                        }
                    });
                } else if (data.translatedText && originalText) {
                    const toLangs = getSelectedLanguages();
                    addHistoryItem(
                        originalText,
                        fromLang,
                        toLangs.join(', '),
                        data.translatedText
                    );
                }

                updateStatus('Translated', '✅ Translation complete', false);
                updateTranslationInfo(
                    data.detectedLanguage || state.detectedLanguage,
                    getSelectedLanguages(),
                    elements.fromLanguage?.value || 'auto'
                );
                updateProgress(100);
                break;

            case 'TranslationFailed':
                showError(`Translation failed: ${data.message}`);
                updateProgress(0);
                break;

            case 'NetworkError':
                showError('Network error - check connection');
                updateProgress(0);
                break;

            case 'TimeoutError':
                showError('Timeout - please try again');
                updateProgress(0);
                break;

            case 'ApiError':
                showError(`API Error: ${data.message}`);
                updateProgress(0);
                break;

            case 'ServerError':
                showError(`Server Error: ${data.message}`);
                updateProgress(0);
                break;

            case 'Info':
                updateStatus('Info', data.message || 'ℹ️ Info', false);
                break;

            case 'Warning':
                updateStatus('Warning', `⚠️ ${data.message}`, false);
                break;

            case 'Success':
                updateStatus('Success', `✅ ${data.message}`, false);
                break;

            default:
                if (data.message) {
                    updateStatus('Info', data.message, data.isError);
                }
                break;
        }
    }

    async function sendTranslationRequest(text) {
        const previousText = state.currentText;
        const toLanguages = getSelectedLanguages();

        try {
            if (!text || text.trim().length === 0) {
                if (isSignalRConnected()) {
                    await window.signalRClient.invoke("ClearText");
                }
                state.currentText = '';
                return;
            }

            if (toLanguages.length === 0) {
                updateStatus('Warning', '⚠️ Select at least one target language', false);
                return;
            }

            if (!isSignalRConnected()) {
                await fallbackTranslate(text);
                state.currentText = text;
                return;
            }

            const action = text.length < previousText.length ? 'delete' : 'type';

            await window.signalRClient.invoke("ProcessText", {
                text: text,
                fromLanguage: elements.fromLanguage?.value || 'auto',
                toLanguages: toLanguages,
                action: action
            });

            state.currentText = text;
        } catch (error) {
            showError(`Failed to send request: ${error.message}`);
            await fallbackTranslate(text);
            state.currentText = text;
        }
    }

    async function fallbackTranslate(text) {
        try {
            const toLanguages = getSelectedLanguages();

            const response = await fetch("/Home/Translate", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    text: text,
                    fromLanguage: elements.fromLanguage?.value || 'auto',
                    toLanguages: toLanguages
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const result = await response.json();

            if (result && result.length > 0) {
                const translations = result
                    .flatMap(root => root.translations || [])
                    .map(t => `${getLanguageName(t.to)}: ${t.text}`)
                    .join("\n\n");

                setTranslationText(translations || "No translation");

                updateStatus('Translated', '✅ Translation complete (HTTP fallback)', false);

                const detectedLang = result[0]?.detectedLanguage?.language;
                if (detectedLang) {
                    state.detectedLanguage = detectedLang;
                }

                updateTranslationInfo(
                    detectedLang,
                    toLanguages,
                    elements.fromLanguage?.value || 'auto'
                );
                updateProgress(100);
            }
        } catch (error) {
            setTranslationText("Translation failed");
            updateProgress(0);
            showError('Translation failed - please try again');
        }
    }

    let translationTimer = null;

    function handleTextChange(text) {
        updateCharCount();
        clearTimeout(translationTimer);

        if (!text || text.trim().length === 0) {
            state.currentText = '';

            if (isSignalRConnected()) {
                window.signalRClient.invoke("ClearText").catch(() => { });
            }

            setTranslationText('');
            updateStatus('Idle', '📝 Ready for input', false);
            updateTranslationInfo(null, null);
            updateProgress(0);
            return;
        }

        if (text.length < CONFIG.minTextLength) {
            updateStatus('TooShort', '📏 Please enter at least 2 characters', false);
            return;
        }

        updateStatus('Typing', '✍️ Typing...', false);

        translationTimer = setTimeout(() => {
            sendTranslationRequest(text);
        }, CONFIG.debounceDelay);
    }

    function clearTranslationUI() {
        setTranslationText('');
        state.lastTranslation = '';
        updateStatus('Idle', '📝 Ready for input', false);
        updateTranslationInfo(null, null);
        updateProgress(0);
    }

    function getHistory() {
        try {
            return JSON.parse(localStorage.getItem('translationHistory')) || [];
        } catch {
            return [];
        }
    }

    function saveHistory(history) {
        try {
            localStorage.setItem('translationHistory', JSON.stringify(history));
        } catch (e) {
            console.warn('Failed to save history:', e);
        }
    }

    function addHistoryItem(originalText, fromLanguage, toLanguage, translatedText) {
        if (!originalText || !translatedText) return;

        const history = getHistory();
        const item = {
            id: Date.now(),
            originalText: originalText.substring(0, 200),
            fromLanguage: fromLanguage || 'auto',
            toLanguage: toLanguage,
            translatedText: translatedText.substring(0, 200),
            timestamp: new Date().toISOString()
        };

        history.unshift(item);

        if (history.length > CONFIG.maxHistoryItems) {
            history.length = CONFIG.maxHistoryItems;
        }

        saveHistory(history);
        renderHistory();
    }

    function clearHistory() {
        if (confirm('Clear all translation history?')) {
            saveHistory([]);
            renderHistory();
        }
    }

    function deleteHistoryItem(id) {
        const history = getHistory();
        const filtered = history.filter(item => item.id !== Number(id));
        saveHistory(filtered);
        renderHistory();
    }

    function renderHistory() {
        const list = elements.historyList;
        if (!list) return;

        const history = getHistory();

        if (history.length === 0) {
            list.innerHTML = `
                <div class="history-empty">
                    <span>No translations yet</span>
                    <span>Your translations will appear here</span>
                </div>
            `;
            return;
        }

        list.innerHTML = history.map(item => `
            <div class="history-item" data-id="${item.id}">
                <div class="history-item-header">
                    <div class="history-item-langs">
                        <span class="history-lang from">${getLanguageName(item.fromLanguage)}</span>
                        <span class="history-arrow">→</span>
                        <span class="history-lang to">${getLanguageName(item.toLanguage)}</span>
                    </div>
                    <div class="history-item-actions">
                        <button class="history-copy-btn" onclick="copyHistoryText('${escapeJs(item.translatedText)}')" title="Copy translation">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="9" y="9" width="13" height="13" rx="2"/>
                                <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"/>
                            </svg>
                        </button>
                        <button class="history-delete-btn" onclick="deleteHistoryItem(${item.id})" title="Delete">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M3 6h18M8 6V4a1 1 0 011-1h6a1 1 0 011 1v2M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/>
                            </svg>
                        </button>
                    </div>
                </div>
                <div class="history-item-text">
                    <div class="history-original">${escapeHtml(item.originalText)}</div>
                    <div class="history-translated">${escapeHtml(item.translatedText)}</div>
                </div>
                <div class="history-item-time">${formatTime(item.timestamp)}</div>
            </div>
        `).join('');
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function escapeJs(text) {
        return text.replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/"/g, '\\"').replace(/\n/g, '\\n');
    }

    function formatTime(timestamp) {
        const date = new Date(timestamp);
        const now = new Date();
        const diff = now - date;

        if (diff < 60000) return 'Just now';
        if (diff < 3600000) return `${Math.floor(diff / 60000)}m ago`;
        if (diff < 86400000) return `${Math.floor(diff / 3600000)}h ago`;
        if (diff < 604800000) return `${Math.floor(diff / 86400000)}d ago`;

        return date.toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: now.getFullYear() !== date.getFullYear() ? 'numeric' : undefined
        });
    }

    function copyHistoryText(text) {
        navigator.clipboard.writeText(text).then(() => {
            const btn = document.activeElement;
            if (btn && btn.classList.contains('history-copy-btn')) {
                btn.innerHTML = '✅';
                setTimeout(() => {
                    btn.innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <rect x="9" y="9" width="13" height="13" rx="2"/>
                        <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"/>
                    </svg>`;
                }, 1500);
            }
        }).catch(() => {
            const textarea = document.createElement('textarea');
            textarea.value = text;
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);
        });
    }

    function initEventListeners() {
        if (elements.sourceText) {
            elements.sourceText.addEventListener("input", () => {
                handleTextChange(elements.sourceText.value);
            });
        }

        if (elements.clearButton) {
            elements.clearButton.addEventListener("click", () => {
                if (!elements.sourceText) return;

                elements.sourceText.value = '';
                state.currentText = '';
                updateCharCount();
                clearTimeout(translationTimer);

                if (isSignalRConnected()) {
                    window.signalRClient.invoke("ClearText").catch(() => {
                        clearTranslationUI();
                    });
                } else {
                    clearTranslationUI();
                }
            });
        }

        if (elements.copyButton) {
            elements.copyButton.addEventListener("click", async () => {
                if (!elements.translatedText) return;

                const text = elements.translatedText.value;

                if (!text || text === 'Your translation will appear here...') {
                    return;
                }

                try {
                    await navigator.clipboard.writeText(text);
                    elements.copyButton.textContent = "✅ Copied!";
                    elements.copyButton.classList.add('copied');

                    setTimeout(() => {
                        elements.copyButton.textContent = "Copy";
                        elements.copyButton.classList.remove('copied');
                    }, 2000);
                } catch {
                    elements.translatedText.select();
                    document.execCommand("copy");
                    elements.copyButton.textContent = "✅ Copied!";
                    elements.copyButton.classList.add('copied');

                    setTimeout(() => {
                        elements.copyButton.textContent = "Copy";
                        elements.copyButton.classList.remove('copied');
                    }, 2000);
                }
            });
        }

        if (elements.fromLanguage) {
            elements.fromLanguage.addEventListener("change", () => {
                const text = elements.sourceText?.value || '';
                const trimmedText = text.trim();

                if (trimmedText && trimmedText.length >= CONFIG.minTextLength) {
                    clearTimeout(translationTimer);
                    sendTranslationRequest(trimmedText);
                } else if (trimmedText && trimmedText.length < CONFIG.minTextLength) {
                    updateStatus('TooShort', '📏 Please enter at least 2 characters', false);
                }

                updateTranslationInfo(
                    state.detectedLanguage,
                    getSelectedLanguages(),
                    elements.fromLanguage?.value || 'auto'
                );
            });
        }

        document.querySelectorAll("#languageOptions input[type='checkbox']").forEach(checkbox => {
            checkbox.addEventListener("change", () => {
                const selected = getSelectedLanguages();

                if (elements.selectedLanguagesText) {
                    elements.selectedLanguagesText.textContent = selected.length === 0
                        ? "Select languages"
                        : selected.map(getLanguageName).join(", ");
                }

                updateTranslationInfo(
                    state.detectedLanguage,
                    selected,
                    elements.fromLanguage?.value || 'auto'
                );

                const text = elements.sourceText?.value || '';
                const trimmedText = text.trim();

                if (trimmedText && trimmedText.length >= CONFIG.minTextLength && selected.length > 0) {
                    clearTimeout(translationTimer);
                    sendTranslationRequest(trimmedText);
                } else if (trimmedText && trimmedText.length < CONFIG.minTextLength) {
                    updateStatus('TooShort', '📏 Please enter at least 2 characters', false);
                } else if (!trimmedText) {
                    updateStatus('Idle', '📝 Ready for input', false);
                }
            });
        });

        if (elements.languageDropdownButton) {
            elements.languageDropdownButton.addEventListener("click", event => {
                event.stopPropagation();

                if (elements.languageOptions) {
                    elements.languageOptions.classList.toggle("open");
                }
            });
        }

        document.addEventListener("click", event => {
            const dropdown = document.querySelector(".language-dropdown");
            const options = elements.languageOptions;

            if (dropdown && options && !dropdown.contains(event.target)) {
                options.classList.remove("open");
            }
        });

        if (elements.sourceText) {
            elements.sourceText.addEventListener("keydown", event => {
                if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
                    event.preventDefault();
                    clearTimeout(translationTimer);
                    sendTranslationRequest(elements.sourceText.value);
                }

                if (event.key === "Escape") {
                    elements.clearButton?.click();
                }
            });
        }

        if (elements.clearHistoryBtn) {
            elements.clearHistoryBtn.addEventListener('click', clearHistory);
        }
    }

    function initSignalR() {
        if (!window.signalRClient) {
            console.error('❌ SignalRClient not found!');
            return;
        }

        window.signalRClient.initialize();

        window.signalRClient.onConnected((data) => {
            state.isConnected = true;

            if (data?.sessionId && elements.sessionId) {
                elements.sessionId.textContent = `Session: ${data.sessionId.substring(0, 8)}`;
            }

            updateConnectionStatus(true, 'Connected');
            updateStatus('Idle', '📝 Ready for input', false);
            updateTranslationInfo(
                state.detectedLanguage,
                getSelectedLanguages(),
                elements.fromLanguage?.value || 'auto'
            );
        });

        window.signalRClient.onTextTranslation((data) => {
            handleTranslationStatus(data);
        });

        window.signalRClient.onError((error) => {
            showError(error?.message || "Unknown error");
        });

        window.addEventListener('signalr-reconnecting', () => {
            updateStatus('Info', '🔄 Reconnecting...', false);
            updateConnectionStatus(false, 'Reconnecting...');
        });

        window.addEventListener('signalr-reconnected', () => {
            state.isConnected = true;
            updateConnectionStatus(true, 'Connected');
            updateStatus('Idle', '✅ Reconnected - Ready', false);
        });

        window.addEventListener('signalr-disconnected', () => {
            state.isConnected = false;
            updateConnectionStatus(false, 'Disconnected');
            updateStatus('Error', '❌ Disconnected - please refresh', true);
        });
    }

    function init() {
        updateStatus('Idle', '📝 Ready for input', false);
        updateTranslationInfo(null, null);
        updateConnectionStatus(false, 'Connecting...');

        const initialSelected = getSelectedLanguages();

        if (elements.selectedLanguagesText) {
            elements.selectedLanguagesText.textContent = initialSelected.length === 0
                ? "Select languages"
                : initialSelected.map(getLanguageName).join(", ");
        }

        updateCharCount();
        initEventListeners();
        initSignalR();
        renderHistory();

        console.log('🚀 Translation app initialized with unified SignalR client');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();