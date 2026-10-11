(() => {
    const select = document.getElementById('TrainingProgramId');
    if (!select) return;
    const update = () => {
        document.querySelectorAll('[data-program-review]').forEach(item => { item.hidden = item.dataset.programReview !== select.value; });
        const empty = document.querySelector('[data-program-review-empty]');
        if (empty) empty.hidden = !!select.value;
    };
    select.addEventListener('change', update);
    update();
})();
