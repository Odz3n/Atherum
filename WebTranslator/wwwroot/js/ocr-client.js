class OcrClient {
    constructor() {
        this.selectedFile = null;
        this.isProcessing = false;
        this.imageData = null;

        this.init();
    }

    init() {
        if (!window.signalRClient) {
            console.error('❌ SignalRClient not found!');
            return;
        }

        window.signalRClient.initialize();

        window.signalRClient.onImageTranslation((data) => {
            this.handleImageStatus(data);
        });

        window.signalRClient.onError((error) => {
            this.showError(error.message);
            this.isProcessing = false;
            this.showTranslateButton(true);
        });

        this.setupDropZone();
        this.setupFileInput();
        this.setupTranslateButton();
        this.showTranslateButton(false);

        console.log('📷 OCR Client initialized with unified SignalR client');
    }

    setupDropZone() {
        const dropZone = document.getElementById('dropZone');
        if (!dropZone) return;

        dropZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropZone.classList.add('drag-over');
        });

        dropZone.addEventListener('dragleave', () => {
            dropZone.classList.remove('drag-over');
        });

        dropZone.addEventListener('drop', (e) => {
            e.preventDefault();
            dropZone.classList.remove('drag-over');

            if (e.dataTransfer.files.length > 0) {
                this.handleFile(e.dataTransfer.files[0]);
            }
        });

        dropZone.addEventListener('click', () => {
            document.getElementById('fileInput')?.click();
        });
    }

    setupFileInput() {
        const fileInput = document.getElementById('fileInput');
        if (!fileInput) return;

        fileInput.addEventListener('change', (e) => {
            if (e.target.files.length > 0) {
                this.handleFile(e.target.files[0]);
            }
        });
    }

    handleFile(file) {
        if (!file.type.startsWith('image/')) {
            this.showError('Please upload an image file');
            return;
        }

        if (file.size > 10 * 1024 * 1024) {
            this.showError('Image too large (max 10MB)');
            return;
        }

        this.selectedFile = file;
        this.showImagePreview(file);
        this.updateFileName(file);
        this.showTranslateButton(true);
        this.clearResults();
        this.showStatus('Image loaded. Press "Translate" to start.', 'info');
    }

    showImagePreview(file) {
        const reader = new FileReader();

        reader.onload = (e) => {
            this.imageData = e.target.result;

            const preview = document.getElementById('imagePreview');
            const container = document.getElementById('imagePreviewSection');

            if (preview) {
                preview.src = this.imageData;
                preview.style.display = 'block';
            }

            if (container) {
                container.style.display = 'block';
                container.classList.add('show');
            }
        };

        reader.readAsDataURL(file);
    }

    updateFileName(file) {
        const fileName = document.getElementById('fileName');

        if (fileName) {
            const size = (file.size / 1024).toFixed(1);
            fileName.textContent = `${file.name} (${size} KB)`;
            fileName.style.display = 'block';
        }
    }

    showTranslateButton(enabled) {
        const btn = document.getElementById('translateBtn');

        if (btn) {
            btn.style.display = 'flex';
            btn.disabled = !enabled;
        }
    }

    clearResults() {
        const sections = [
            'extractedTextSection',
            'translationSection',
            'detectedLanguageSection',
            'ocrProgressSection'
        ];

        sections.forEach(id => {
            const el = document.getElementById(id);

            if (el) {
                el.style.display = 'none';
            }
        });

        const confidence = document.getElementById('ocrConfidence');

        if (confidence) {
            confidence.style.display = 'none';
        }

        const bar = document.getElementById('ocrProgressBar');

        if (bar) {
            bar.style.width = '0%';
            bar.style.display = 'none';
        }

        const text = document.getElementById('ocrProgressText');

        if (text) {
            text.style.display = 'none';
        }
    }

    setupTranslateButton() {
        const btn = document.getElementById('translateBtn');

        if (btn) {
            btn.addEventListener('click', () => {
                this.startTranslation();
            });
        }
    }

    async startTranslation() {
        if (!this.selectedFile) {
            this.showError('Please select an image first');
            return;
        }

        if (this.isProcessing) {
            return;
        }

        const toLanguages = this.getToLanguages();

        if (toLanguages.length === 0) {
            this.showError('Please select at least one target language');
            return;
        }

        this.isProcessing = true;
        this.showTranslateButton(false);
        this.showStatus('Processing...', 'processing');

        try {
            await this.uploadFileViaFetch();
        } catch (error) {
            this.showError(error.message);
            this.isProcessing = false;
            this.showTranslateButton(true);
        }
    }

    async uploadFileViaFetch() {
        try {
            const formData = new FormData();

            formData.append('image', this.selectedFile);
            formData.append('fromLanguage', this.getFromLanguage());

            const toLanguages = this.getToLanguages();

            toLanguages.forEach(lang => {
                formData.append('toLanguages', lang);
            });

            const response = await fetch('/Ocr/Upload', {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || 'Upload failed');
            }

            const result = await response.json();
            this.showStatus(result.message || 'Processing...', 'success');

        } catch (error) {
            console.error('Upload error:', error);
            throw error;
        }
    }

    handleImageStatus(data) {
        if (data.ocrProgress !== undefined) {
            const bar = document.getElementById('ocrProgressBar');
            const text = document.getElementById('ocrProgressText');

            if (bar) {
                bar.style.width = data.ocrProgress + '%';
                bar.style.display = 'block';
            }

            if (text) {
                text.textContent = data.ocrProgress + '%';
                text.style.display = 'block';
            }

            document.getElementById('ocrProgressSection').style.display = 'block';
        }

        if (data.ocrConfidence) {
            const confidence = document.getElementById('ocrConfidence');

            if (confidence) {
                confidence.textContent = `Confidence: ${Math.round(data.ocrConfidence * 100)}%`;
                confidence.style.display = 'block';
            }
        }

        if (data.detectedLanguage) {
            const langText = document.getElementById('detectedLanguageText');
            const langSection = document.getElementById('detectedLanguageSection');

            if (langText) {
                langText.textContent = this.getLanguageName(data.detectedLanguage);
            }

            if (langSection) {
                langSection.style.display = 'block';
            }
        }

        if (data.extractedText || data.originalText) {
            const text = data.extractedText || data.originalText;
            const extractedTextEl = document.getElementById('extractedText');
            const extractedSection = document.getElementById('extractedTextSection');

            if (extractedTextEl) {
                extractedTextEl.textContent = text;
            }

            if (extractedSection) {
                extractedSection.style.display = 'block';
            }
        }

        const hasTranslations = data.allTranslations && data.allTranslations.length > 0;
        const hasTranslatedText = data.translatedText && data.translatedText.length > 0;

        if (hasTranslations || hasTranslatedText) {
            const resultEl = document.getElementById('translationResult');
            const section = document.getElementById('translationSection');

            if (resultEl) {
                if (hasTranslations) {
                    const translationsHtml = data.allTranslations
                        .map(t => {
                            const langName = this.getLanguageName(t.toLanguage);

                            return `<div class="translation-item">
                                <span class="lang">${langName}:</span>
                                <span class="text">${t.text}</span>
                            </div>`;
                        })
                        .join('');

                    resultEl.innerHTML = translationsHtml;
                    resultEl.className = 'translation-result multiple';
                } else if (hasTranslatedText) {
                    resultEl.textContent = data.translatedText;
                    resultEl.className = 'translation-result single';
                }

                if (section) {
                    section.style.display = 'block';
                }
            }

            this.isProcessing = false;
            this.showTranslateButton(true);
            this.showStatus('✅ Translation complete!', 'success');
        }

        if (data.isError) {
            this.isProcessing = false;
            this.showTranslateButton(true);
            this.showError(data.message || 'Processing failed');
        }
    }

    getFromLanguage() {
        return document.getElementById('fromLanguage')?.value || 'auto';
    }

    getToLanguages() {
        return Array.from(
            document.querySelectorAll('input[name="toLanguages"]:checked')
        ).map(cb => cb.value);
    }

    getLanguageName(code) {
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

    showStatus(message, type = 'info') {
        const status = document.getElementById('uploadStatus');

        if (status) {
            status.textContent = message;
            status.className = `status-${type}`;
            status.style.display = 'block';
        }
    }

    showError(message) {
        const error = document.getElementById('ocrError');

        if (error) {
            error.textContent = '❌ ' + message;
            error.style.display = 'block';
            error.className = 'error';

            setTimeout(() => {
                error.style.display = 'none';
            }, 5000);
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.ocrClient = new OcrClient();
});