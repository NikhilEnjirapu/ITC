(() => {
    const form = document.querySelector('[data-video-upload]');
    if (!form) return;
    const input = form.querySelector('#videoFile');
    const preview = form.querySelector('#video-preview');
    const status = form.querySelector('#video-file-status');
    let url;
    input.addEventListener('change', () => {
        if (url) URL.revokeObjectURL(url);
        preview.removeAttribute('src');
        preview.hidden = true;
        input.setCustomValidity('');
        const file = input.files[0];
        status.textContent = '';
        if (!file) return;
        if (!/\.(mp4|webm)$/i.test(file.name) || !file.size || file.size > 500 * 1024 * 1024) {
            status.textContent = 'Choose a non-empty MP4 or WebM video up to 500 MB.';
            input.setCustomValidity(status.textContent);
            input.reportValidity();
            return;
        }
        status.textContent = `${file.name} (${(file.size / 1024 / 1024).toFixed(1)} MB)`;
        url = URL.createObjectURL(file);
        preview.src = url;
        preview.hidden = false;
    });
    preview.addEventListener('loadedmetadata', () => {
        if (Number.isFinite(preview.duration) && preview.duration > 0)
            form.querySelector('#DurationSeconds').value = Math.ceil(preview.duration);
    });
    preview.addEventListener('error', () => {
        status.textContent = 'This browser could not preview the video. Check the file and enter its duration manually.';
    });
    form.addEventListener('submit', event => {
        const file = input.files[0];
        // jQuery validation adds novalidate to the form, so explicitly cancel
        // invalid submissions rather than relying on native browser validation.
        if (file && (!/\.(mp4|webm)$/i.test(file.name) || !file.size || file.size > 500 * 1024 * 1024)) {
            event.preventDefault();
            status.textContent = `Selected file: ${(file.size / 1024 / 1024).toFixed(1)} MB. Choose a non-empty MP4 or WebM video up to 500 MB.`;
            input.setCustomValidity(status.textContent);
            input.reportValidity();
            return;
        }
        input.setCustomValidity('');
        if (window.jQuery && !window.jQuery(form).valid()) { event.preventDefault(); return; }
        if (!file && !form.querySelector('#VideoUrl').value.trim()) {
            event.preventDefault();
            input.setCustomValidity('Choose a video to upload.');
            input.reportValidity();
            return;
        }
        if (!form.checkValidity()) { event.preventDefault(); form.reportValidity(); return; }
        form.querySelector('#video-save-status').hidden = false;
        form.querySelector('button').disabled = true;
    });
    form.querySelector('#VideoUrl').addEventListener('input', () => input.setCustomValidity(''));
    window.addEventListener('pageshow', () => {
        form.querySelector('button').disabled = false;
        form.querySelector('#video-save-status').hidden = true;
    });
})();
