class AnalysisClient {
    constructor() {
        this.selectedFile = null;
        this.isProcessing = false;
        this.imageData = null;
        this.errorTimeout = null;
        this.boundingBoxesRendered = false;

        this.init();
    }

    init() {
        if (!window.signalRClient) {
            console.error('SignalRClient not found');
            return;
        }

        try {
            window.signalRClient.initialize();

            window.signalRClient.onImageAnalysis((data) => {
                this.handleAnalysisStatus(data);
            });

            window.signalRClient.onAnalysisError((error) => {
                this.isProcessing = false;
                this.updateAnalyzeButton();
                this.showError(error?.message || 'Image analysis failed');
            });

            window.signalRClient.onError((error) => {
                this.isProcessing = false;
                this.updateAnalyzeButton();
                this.showError(error?.message || 'Translation error');
            });

            this.setupDropZone();
            this.setupFileInput();
            this.setupAnalyzeButton();
            this.setupOptions();
            this.setupPresets();

            this.updateAnalyzeButton();
        } catch (error) {
            console.error('Image analysis initialization error:', error);
            this.showError(error?.message || 'Failed to initialize image analysis');
        }
    }

    setupDropZone() {
        const dropZone = document.getElementById('dropZone');

        if (!dropZone) {
            return;
        }

        dropZone.addEventListener('dragover', (event) => {
            event.preventDefault();
            dropZone.classList.add('drag-over');
        });

        dropZone.addEventListener('dragleave', (event) => {
            if (!dropZone.contains(event.relatedTarget)) {
                dropZone.classList.remove('drag-over');
            }
        });

        dropZone.addEventListener('drop', (event) => {
            event.preventDefault();
            dropZone.classList.remove('drag-over');

            const file = event.dataTransfer?.files?.[0];

            if (file) {
                this.handleFile(file);
            }
        });

        dropZone.addEventListener('click', () => {
            if (!this.isProcessing) {
                document.getElementById('fileInput')?.click();
            }
        });

        dropZone.addEventListener('keydown', (event) => {
            if (
                (event.key === 'Enter' || event.key === ' ') &&
                !this.isProcessing
            ) {
                event.preventDefault();
                document.getElementById('fileInput')?.click();
            }
        });
    }

    setupFileInput() {
        const fileInput = document.getElementById('fileInput');

        if (!fileInput) {
            return;
        }

        fileInput.addEventListener('change', (event) => {
            const file = event.target.files?.[0];

            if (file) {
                this.handleFile(file);
            }
        });
    }

    handleFile(file) {
        if (!file) {
            return;
        }

        if (!file.type || !file.type.startsWith('image/')) {
            this.showError('Please upload an image file');
            return;
        }

        if (file.size > 10 * 1024 * 1024) {
            this.showError('Image too large (max 10MB)');
            return;
        }

        if (this.isProcessing) {
            this.showError('Please wait until the current analysis is complete');
            return;
        }

        this.selectedFile = file;
        this.clearResults();
        this.clearError();
        this.updateFileInfo(file);
        this.showImagePreview(file);
        this.updateAnalyzeButton();
        this.showStatus(
            'Image loaded. Configure options and press "Analyze".',
            'info'
        );
    }

    ensureImageWrapper() {
        const preview = document.getElementById('imagePreview');

        if (!preview) {
            return null;
        }

        let wrapper = preview.parentElement;

        if (!wrapper || !wrapper.classList.contains('bounding-box-container')) {
            wrapper = document.createElement('div');
            wrapper.className = 'bounding-box-container';
            preview.replaceWith(wrapper);
            wrapper.appendChild(preview);
        }

        return wrapper;
    }

    showImagePreview(file) {
        const reader = new FileReader();

        reader.onload = (event) => {
            const result = event.target?.result;

            if (typeof result !== 'string') {
                this.showError('Failed to read image');
                return;
            }

            this.imageData = result;

            const preview = document.getElementById('imagePreview');

            if (preview) {
                preview.src = result;
                preview.style.display = 'block';
            }

            this.ensureImageWrapper();

            const section = document.getElementById('imageSection');

            if (section) {
                section.style.display = 'block';
            }
        };

        reader.onerror = () => {
            this.showError('Failed to read image');
        };

        reader.readAsDataURL(file);
    }

    updateFileInfo(file) {
        const info = document.getElementById('fileInfo');
        const name = document.getElementById('fileName');

        if (info && name) {
            const size = (file.size / 1024).toFixed(1);
            name.textContent = `${file.name} (${size} KB)`;
            info.style.display = 'flex';
        }

        const removeButton = document.getElementById('fileRemove');

        if (removeButton) {
            removeButton.onclick = () => {
                this.removeFile();
            };
        }
    }

    removeFile() {
        if (this.isProcessing) {
            return;
        }

        this.selectedFile = null;
        this.imageData = null;

        const input = document.getElementById('fileInput');

        if (input) {
            input.value = '';
        }

        const info = document.getElementById('fileInfo');

        if (info) {
            info.style.display = 'none';
        }

        const preview = document.getElementById('imagePreview');

        if (preview) {
            preview.src = '';
            preview.style.display = 'none';
        }

        const imageSection = document.getElementById('imageSection');

        if (imageSection) {
            imageSection.style.display = 'none';
        }

        this.clearResults();
        this.clearError();
        this.showStatus('', '');
        this.updateAnalyzeButton();
    }

    setupAnalyzeButton() {
        const button = document.getElementById('analyzeBtn');

        if (!button) {
            return;
        }

        button.addEventListener('click', () => {
            this.startAnalysis();
        });

        this.updateAnalyzeButton();
    }

    updateAnalyzeButton() {
        const button = document.getElementById('analyzeBtn');

        if (!button) {
            return;
        }

        const hasFile = this.selectedFile !== null;
        const hasFeatures =
            document.querySelectorAll(
                '.feature-checkboxes input:checked'
            ).length > 0;

        const hasLanguages =
            document.querySelectorAll(
                'input[name="toLanguages"]:checked'
            ).length > 0;

        const canAnalyze =
            hasFile &&
            hasFeatures &&
            hasLanguages &&
            !this.isProcessing;

        button.disabled = !canAnalyze;
        button.classList.toggle('enabled', canAnalyze);
    }

    setupOptions() {
        const range = document.getElementById('minConfidence');
        const value = document.getElementById('confidenceValue');

        if (range && value) {
            const updateConfidence = () => {
                value.textContent = `${range.value}%`;
            };

            range.addEventListener('input', updateConfidence);
            updateConfidence();
        }

        document
            .querySelectorAll(
                '.feature-checkboxes input, ' +
                '.process-checkboxes input, ' +
                'input[name="toLanguages"], ' +
                '#fromLanguage'
            )
            .forEach((element) => {
                element.addEventListener('change', () => {
                    this.updateAnalyzeButton();
                });
            });
    }

    setupPresets() {
        document.querySelectorAll('.preset-btn').forEach((button) => {
            button.addEventListener('click', () => {
                this.applyPreset(button.dataset.preset);
            });
        });
    }

    applyPreset(preset) {
        if (this.isProcessing) {
            return;
        }

        const ocr = document.getElementById('featureOcr');
        const objects = document.getElementById('featureObjects');
        const tags = document.getElementById('featureTags');

        const translateText =
            document.getElementById('processTranslateText');

        const translateObjects =
            document.getElementById('processTranslateObjects');

        const translateTags =
            document.getElementById('processTranslateTags');

        const confidence =
            document.getElementById('minConfidence');

        const confidenceValue =
            document.getElementById('confidenceValue');

        const maxResults =
            document.getElementById('maxResults');

        if (ocr) ocr.checked = false;
        if (objects) objects.checked = false;
        if (tags) tags.checked = false;

        if (translateText) translateText.checked = false;
        if (translateObjects) translateObjects.checked = false;
        if (translateTags) translateTags.checked = false;

        switch (preset) {
            case 'all':
                if (ocr) ocr.checked = true;
                if (objects) objects.checked = true;
                if (tags) tags.checked = true;

                if (translateText) translateText.checked = true;
                if (translateObjects) translateObjects.checked = true;
                if (translateTags) translateTags.checked = true;

                if (confidence) confidence.value = '50';
                if (confidenceValue) confidenceValue.textContent = '50%';
                if (maxResults) maxResults.value = '30';
                break;

            case 'text':
                if (ocr) ocr.checked = true;
                if (translateText) translateText.checked = true;

                if (confidence) confidence.value = '50';
                if (confidenceValue) confidenceValue.textContent = '50%';
                if (maxResults) maxResults.value = '50';
                break;

            case 'objects':
                if (objects) objects.checked = true;
                if (tags) tags.checked = true;

                if (translateObjects) translateObjects.checked = true;
                if (translateTags) translateTags.checked = true;

                if (confidence) confidence.value = '60';
                if (confidenceValue) confidenceValue.textContent = '60%';
                if (maxResults) maxResults.value = '20';
                break;

            case 'tags':
                if (tags) tags.checked = true;
                if (translateTags) translateTags.checked = true;

                if (confidence) confidence.value = '40';
                if (confidenceValue) confidenceValue.textContent = '40%';
                if (maxResults) maxResults.value = '30';
                break;
        }

        this.updateAnalyzeButton();
    }

    getAnalysisOptions() {
        const features = [];

        if (document.getElementById('featureOcr')?.checked) {
            features.push(1);
        }

        if (document.getElementById('featureObjects')?.checked) {
            features.push(2);
        }

        if (document.getElementById('featureTags')?.checked) {
            features.push(4);
        }

        const minConfidence = Number.parseInt(
            document.getElementById('minConfidence')?.value || '50',
            10
        );

        const maxResults = Number.parseInt(
            document.getElementById('maxResults')?.value || '30',
            10
        );

        const fromLanguage = this.getFromLanguage();
        const toLanguages = this.getToLanguages();

        const saveToBlob =
            document.getElementById('processSaveToBlob')?.checked === true;

        return {
            features: features.reduce((sum, value) => sum + value, 0),
            minConfidence: Math.min(100, Math.max(0, minConfidence)) / 100,
            maxResults: Math.min(100, Math.max(1, maxResults)),
            includeBoundingBoxes: true,

            langFrom: fromLanguage,
            langsTo: toLanguages,

            processing: {
                translateExtractedText:
                    document.getElementById('processTranslateText')?.checked === true,

                translateObjectNames:
                    document.getElementById('processTranslateObjects')?.checked === true,

                translateTagNames:
                    document.getElementById('processTranslateTags')?.checked === true,

                saveToBlob: saveToBlob,

                returnDownloadUrl: saveToBlob,

                langFrom: fromLanguage,
                langsTo: toLanguages
            }
        };
    }

    async startAnalysis() {
        if (this.isProcessing) {
            return;
        }

        if (!this.selectedFile) {
            this.showError('Please select an image first');
            return;
        }

        const toLanguages = this.getToLanguages();

        if (toLanguages.length === 0) {
            this.showError('Please select at least one target language');
            return;
        }

        const featureCount =
            document.querySelectorAll(
                '.feature-checkboxes input:checked'
            ).length;

        if (featureCount === 0) {
            this.showError('Please select at least one analysis feature');
            return;
        }

        this.isProcessing = true;
        this.clearError();
        this.clearAnalysisResults();
        this.updateAnalyzeButton();
        this.showStatus('🔄 Processing...', 'processing');
        this.updateProgress(0);

        try {
            await this.sendForAnalysis();
        } catch (error) {
            console.error('Analysis error:', error);

            this.isProcessing = false;
            this.updateAnalyzeButton();

            this.showError(
                error?.message || 'Failed to start image analysis'
            );
        }
    }

    async sendForAnalysis() {
        if (!this.selectedFile) {
            throw new Error('Please select an image first');
        }

        if (!window.signalRClient) {
            throw new Error('SignalR client is not available');
        }

        const imageData = await this.readFileAsDataUrl(
            this.selectedFile
        );

        this.imageData = imageData;

        const options = this.getAnalysisOptions();

        await window.signalRClient.invoke(
            'ProcessImageAnalysis',
            {
                imageData,
                fromLanguage: this.getFromLanguage(),
                toLanguages: this.getToLanguages(),
                options
            }
        );
    }

    readFileAsDataUrl(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();

            reader.onload = () => {
                if (typeof reader.result !== 'string') {
                    reject(new Error('Failed to read image'));
                    return;
                }

                resolve(reader.result);
            };

            reader.onerror = () => {
                reject(new Error('Failed to read image'));
            };

            reader.onabort = () => {
                reject(new Error('Image reading was aborted'));
            };

            reader.readAsDataURL(file);
        });
    }

    handleAnalysisStatus(data) {
        if (!data) {
            return;
        }

        if (data.progress !== undefined) {
            this.updateProgress(data.progress);
        }

        if (data.imagePreview && data.imagePreview !== this.imageData) {
            this.imageData = data.imagePreview;
            this.updateMainImage(data.imagePreview);
        } else if (!data.imagePreview && this.imageData) {
            this.updateMainImage(this.imageData);
        }

        if (data.message) {
            this.showStatus(
                data.message,
                data.isError ? 'error' : 'processing'
            );
        }

        const normalizedType =
            typeof data.type === 'string'
                ? data.type.toLowerCase()
                : data.type;

        if (normalizedType === 'saving') {
            this.showStatus(
                data.message || '💾 Saving to cloud storage...',
                'processing'
            );

            if (data.progress !== undefined) {
                this.updateProgress(data.progress);
            }

            if (data.downloadUrl || data.blobUri) {
                const link = document.getElementById('blobLink');
                if (link) {
                    link.href = data.downloadUrl || data.blobUri;
                    link.textContent = '📎 Download from cloud';
                    link.style.display = 'inline-block';
                }
                const section = document.getElementById('blobSection');
                if (section) {
                    section.style.display = 'block';
                }
            }

            return;
        }

        if (Array.isArray(data.objects)) {
            this.renderObjects(data.objects);
        }

        if (Array.isArray(data.objects) &&
            data.objects.length > 0 &&
            !this.boundingBoxesRendered) {
            this.renderBoundingBoxes(data.objects);
        }

        if (Array.isArray(data.tags)) {
            this.renderTags(data.tags);
        }

        if (data.extractedText !== undefined) {
            const element =
                document.getElementById('extractedText');

            if (element) {
                element.textContent =
                    data.extractedText || '';
            }

            const section =
                document.getElementById('extractedTextSection');

            if (section) {
                section.style.display =
                    data.extractedText ? 'block' : 'none';
            }
        }

        if (Array.isArray(data.translations) && data.translations.length > 0) {
            this.renderTranslations(data.translations);
        }

        if (data.detectedLanguage) {
            const element =
                document.getElementById('detectedLanguageText');

            if (element) {
                element.textContent =
                    this.getLanguageName(data.detectedLanguage);
            }

            const section =
                document.getElementById(
                    'detectedLanguageSection'
                );

            if (section) {
                section.style.display = 'block';
            }
        }

        const isFailureStatus =
            data.isError ||
            normalizedType === 'translationfailed' ||
            normalizedType === 'validationerror';

        if (isFailureStatus) {
            this.isProcessing = false;
            this.updateAnalyzeButton();

            this.showError(
                data.message || 'Image analysis failed'
            );

            return;
        }

        const numericProgress = Number(data.progress);

        const isSuccessStatus =
            normalizedType === 'translated' ||
            normalizedType === 'success' ||
            (Number.isFinite(numericProgress) && numericProgress >= 100);

        if (isSuccessStatus) {
            this.isProcessing = false;
            this.updateAnalyzeButton();
            this.updateProgress(100);
            this.showStatus(
                '✅ Analysis complete!',
                'success'
            );
        }
    }

    updateMainImage(imageData) {
        if (!imageData) {
            return;
        }

        const preview =
            document.getElementById('imagePreview');

        if (preview && preview.src !== imageData) {
            preview.src = imageData;
        }

        if (preview) {
            preview.style.display = 'block';
        }

        this.ensureImageWrapper();

        const section =
            document.getElementById('imageSection');

        if (section) {
            section.style.display = 'block';
        }
    }

    renderBoundingBoxes(objects) {
        if (this.boundingBoxesRendered) {
            return;
        }

        const wrapper = this.ensureImageWrapper();

        if (!wrapper) {
            return;
        }

        this.clearBoundingBoxes();

        if (!Array.isArray(objects) || objects.length === 0) {
            return;
        }

        objects.forEach((obj) => {
            const box =
                obj?.boundingBox ||
                obj?.box;

            if (!box) {
                return;
            }

            const element =
                document.createElement('div');

            element.className =
                'bounding-box';

            element.style.left =
                `${this.normalizePercentage(box.x)}%`;

            element.style.top =
                `${this.normalizePercentage(box.y)}%`;

            element.style.width =
                `${this.normalizePercentage(box.width)}%`;

            element.style.height =
                `${this.normalizePercentage(box.height)}%`;

            const label =
                document.createElement('span');

            label.className =
                'box-label';

            label.textContent =
                `${obj?.name || 'Object'} ` +
                `(${Math.round(
                    this.normalizeConfidence(obj?.confidence) * 100
                )
                }%)`;

            element.appendChild(label);
            wrapper.appendChild(element);
        });

        this.boundingBoxesRendered = true;

        const section =
            document.getElementById('imageSection');

        if (section) {
            section.style.display = 'block';
        }
    }

    clearBoundingBoxes() {
        const preview = document.getElementById('imagePreview');
        const wrapper = preview?.parentElement;

        if (wrapper && wrapper.classList.contains('bounding-box-container')) {
            wrapper.querySelectorAll('.bounding-box').forEach((element) => {
                element.remove();
            });
        }

        this.boundingBoxesRendered = false;
    }

    renderObjects(objects) {
        const container =
            document.getElementById('objectsContainer');

        if (!container || !Array.isArray(objects)) {
            return;
        }

        container.replaceChildren();

        objects.forEach((obj) => {
            const item =
                document.createElement('div');

            item.className = 'object-item';

            const name =
                document.createElement('span');

            name.className = 'object-name';
            name.textContent =
                obj?.name || 'Unknown object';

            const confidence =
                document.createElement('span');

            confidence.className =
                'object-confidence';

            confidence.textContent =
                `${Math.round(
                    this.normalizeConfidence(obj?.confidence) * 100
                )
                }%`;

            item.appendChild(name);
            item.appendChild(confidence);

            if (obj?.translatedName) {
                const translation =
                    document.createElement('span');

                translation.className =
                    'object-translation';

                translation.textContent =
                    `→ ${obj.translatedName}`;

                item.appendChild(translation);
            }

            container.appendChild(item);
        });

        const count =
            document.getElementById('objectsCount');

        if (count) {
            count.textContent =
                `(${objects.length})`;
        }

        const section =
            document.getElementById('objectsSection');

        if (section) {
            section.style.display =
                objects.length > 0 ? 'block' : 'none';
        }
    }

    renderTags(tags) {
        const container =
            document.getElementById('tagsContainer');

        if (!container || !Array.isArray(tags)) {
            return;
        }

        container.replaceChildren();

        tags.forEach((tag) => {
            const element =
                document.createElement('span');

            element.className = 'tag-item';

            element.appendChild(
                document.createTextNode(
                    tag?.name || 'Unknown tag'
                )
            );

            const confidence =
                document.createElement('span');

            confidence.className =
                'tag-confidence';

            confidence.textContent =
                `${Math.round(
                    this.normalizeConfidence(tag?.confidence) * 100
                )
                }%`;

            element.appendChild(
                document.createTextNode(' ')
            );

            element.appendChild(confidence);

            if (tag?.translatedName) {
                const translation =
                    document.createElement('span');

                translation.className =
                    'tag-translation';

                translation.textContent =
                    `→ ${tag.translatedName}`;

                element.appendChild(
                    document.createTextNode(' ')
                );

                element.appendChild(translation);
            }

            container.appendChild(element);
        });

        const count =
            document.getElementById('tagsCount');

        if (count) {
            count.textContent =
                `(${tags.length})`;
        }

        const section =
            document.getElementById('tagsSection');

        if (section) {
            section.style.display =
                tags.length > 0 ? 'block' : 'none';
        }
    }

    renderTranslations(translations) {
        const container =
            document.getElementById('translationResult');

        if (!container || !Array.isArray(translations)) {
            return;
        }

        container.replaceChildren();

        if (translations.length === 1) {
            container.textContent =
                translations[0]?.text || '';
        } else {
            translations.forEach((translation) => {
                const item =
                    document.createElement('div');

                item.className =
                    'translation-item';

                const language =
                    document.createElement('span');

                language.className = 'lang';

                language.textContent =
                    `${this.getLanguageName(
                        translation?.toLanguage
                    )
                    }:`;

                const text =
                    document.createElement('span');

                text.className = 'text';
                text.textContent =
                    translation?.text || '';

                item.appendChild(language);
                item.appendChild(
                    document.createTextNode(' ')
                );
                item.appendChild(text);

                container.appendChild(item);
            });
        }

        const section =
            document.getElementById('translationSection');

        if (section) {
            section.style.display = 'block';
        }
    }

    updateProgress(progress) {
        const value = Number(progress);

        if (!Number.isFinite(value)) {
            return;
        }

        const normalized =
            Math.min(100, Math.max(0, Math.round(value)));

        const bar =
            document.getElementById('analysisProgress');

        const text =
            document.getElementById(
                'analysisProgressText'
            );

        if (bar) {
            bar.style.width = `${normalized}%`;
        }

        if (text) {
            text.textContent = `${normalized}%`;
        }

        const section =
            document.getElementById('progressSection');

        if (section) {
            section.style.display = 'block';
        }
    }

    getFromLanguage() {
        return (
            document.getElementById('fromLanguage')?.value ||
            'auto'
        );
    }

    getToLanguages() {
        return Array.from(
            document.querySelectorAll(
                'input[name="toLanguages"]:checked'
            )
        ).map((element) => element.value);
    }

    getLanguageName(code) {
        const languages = {
            auto: 'Auto detect',
            en: 'English',
            ru: 'Russian',
            uk: 'Ukrainian',
            bg: 'Bulgarian',
            de: 'German',
            fr: 'French',
            es: 'Spanish'
        };

        return languages[code] || code || 'Unknown';
    }

    normalizeConfidence(value) {
        const number = Number(value);

        if (!Number.isFinite(number)) {
            return 0;
        }

        return number > 1
            ? Math.min(100, Math.max(0, number)) / 100
            : Math.min(1, Math.max(0, number));
    }

    normalizePercentage(value) {
        const number = Number(value);

        if (!Number.isFinite(number)) {
            return 0;
        }

        const percentage = number > 1 ? number : number * 100;

        return Math.min(100, Math.max(0, percentage));
    }

    showStatus(message, type = 'info') {
        const status =
            document.getElementById('uploadStatus');

        if (!status) {
            return;
        }

        if (!message) {
            status.textContent = '';
            status.className = '';
            status.style.display = 'none';
            return;
        }

        status.textContent = message;
        status.className = `status-${type}`;
        status.style.display = 'block';
    }

    showError(message) {
        const error =
            document.getElementById('analysisError');

        if (!error) {
            return;
        }

        clearTimeout(this.errorTimeout);

        error.textContent =
            `❌ ${message || 'An unknown error occurred'}`;

        error.className = 'error';
        error.style.display = 'block';

        this.errorTimeout = setTimeout(() => {
            error.style.display = 'none';
        }, 8000);
    }

    clearError() {
        clearTimeout(this.errorTimeout);

        const error =
            document.getElementById('analysisError');

        if (error) {
            error.textContent = '';
            error.style.display = 'none';
        }
    }

    clearAnalysisResults() {
        const sections = [
            'objectsSection',
            'tagsSection',
            'extractedTextSection',
            'translationSection',
            'detectedLanguageSection',
            'blobSection',
            'progressSection'
        ];

        sections.forEach((id) => {
            const element =
                document.getElementById(id);

            if (element) {
                element.style.display = 'none';
            }
        });

        this.clearBoundingBoxes();

        const objects =
            document.getElementById(
                'objectsContainer'
            );

        if (objects) {
            objects.replaceChildren();
        }

        const tags =
            document.getElementById(
                'tagsContainer'
            );

        if (tags) {
            tags.replaceChildren();
        }

        const text =
            document.getElementById(
                'extractedText'
            );

        if (text) {
            text.textContent = '';
        }

        const translations =
            document.getElementById(
                'translationResult'
            );

        if (translations) {
            translations.replaceChildren();
        }

        const detected =
            document.getElementById(
                'detectedLanguageText'
            );

        if (detected) {
            detected.textContent = '';
        }

        const blobLink =
            document.getElementById('blobLink');

        if (blobLink) {
            blobLink.href = '#';
        }

        const objectCount =
            document.getElementById(
                'objectsCount'
            );

        if (objectCount) {
            objectCount.textContent = '';
        }

        const tagCount =
            document.getElementById(
                'tagsCount'
            );

        if (tagCount) {
            tagCount.textContent = '';
        }

        this.updateProgress(0);

        const imageSection =
            document.getElementById(
                'imageSection'
            );

        if (imageSection && this.imageData) {
            imageSection.style.display = 'block';
        }
    }

    clearResults() {
        this.clearAnalysisResults();
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.analysisClient = new AnalysisClient();
});