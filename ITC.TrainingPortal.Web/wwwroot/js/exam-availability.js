(() => {
    const initial = document.getElementById('examAvailability');
    if (!initial) return;
    const url = initial.dataset.availabilityUrl;
    let busy = false;
    async function refresh() {
        if (busy) return;
        busy = true;
        try {
            const response = await fetch(url, {cache: 'no-store'});
            if (!response.ok || response.redirected) return;
            const html = await response.text();
            const replacement = new DOMParser().parseFromString(html, 'text/html').getElementById('examAvailability');
            const current = document.getElementById('examAvailability');
            if (replacement && current && replacement.outerHTML !== current.outerHTML) current.replaceWith(replacement);
        } catch { /* The existing server-rendered state remains; exam start revalidates access. */ }
        finally { busy = false; }
    }
    setInterval(refresh, 3000);
})();
