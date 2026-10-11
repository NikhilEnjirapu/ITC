(() => {
    if (window.self !== window.top) {
        document.body.classList.add('employee-player-embedded');
        return;
    }

    document.querySelectorAll('[data-watch-fullscreen]').forEach(link => {
        link.addEventListener('click', event => {
            if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            event.preventDefault();
            const overlay = document.createElement('div');
            overlay.className = 'employee-fullscreen';
            overlay.setAttribute('role', 'dialog');
            overlay.setAttribute('aria-modal', 'true');
            overlay.setAttribute('aria-label', 'Training video');
            const frame = document.createElement('iframe');
            frame.title = 'Training video player';
            frame.allow = 'fullscreen';
            overlay.append(frame);
            document.body.append(overlay);
            const shell = document.querySelector('.app-shell');
            if (shell) shell.inert = true;
            document.body.classList.add('employee-fullscreen-open');
            const cleanup = () => {
                overlay.remove();
                if (shell) shell.inert = false;
                document.body.classList.remove('employee-fullscreen-open');
                document.removeEventListener('fullscreenchange', onFullscreenChange);
                document.removeEventListener('keydown', onKeydown);
                link.focus();
            };
            let enteredFullscreen = false;
            const onFullscreenChange = () => {
                if (document.fullscreenElement === overlay) enteredFullscreen = true;
                // Native video fullscreen can make the iframe the fullscreen element.
                // Keep its player alive until fullscreen is actually exited.
                else if (enteredFullscreen && !document.fullscreenElement) cleanup();
            };
            const dismiss = () => {
                if (document.fullscreenElement === overlay) document.exitFullscreen().catch(() => {});
                cleanup();
            };
            const onKeydown = event => {
                if (event.key === 'Escape') dismiss();
            };
            frame.addEventListener('load', () => {
                try { frame.contentDocument.addEventListener('keydown', onKeydown); } catch {}
            });
            document.addEventListener('fullscreenchange', onFullscreenChange);
            document.addEventListener('keydown', onKeydown);
            // Request within the Watch click so browser user-activation is retained.
            // The viewport overlay also works when browser fullscreen is unavailable.
            if (overlay.requestFullscreen) overlay.requestFullscreen().catch(() => {});
            frame.src = link.href;
            frame.focus();
        });
    });
})();
