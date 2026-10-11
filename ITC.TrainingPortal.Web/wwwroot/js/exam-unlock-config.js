(() => {
    const condition = document.getElementById('ExamUnlockCondition');
    const video = document.getElementById('ExamUnlockVideoId');
    if (!condition || !video || !document.getElementById('unlockVideoField')) return;
    function update() {
        const specific = condition.value === '1';
        document.getElementById('unlockVideoField').hidden = !specific;
        video.required = specific;
        video.disabled = !specific;
        if (!specific) video.value = '';
    }
    condition.addEventListener('change', update);
    update();
})();
