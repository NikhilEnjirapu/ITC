(() => {
    const type = document.getElementById('RuleType');
    if (!type) return;
    const update = () => {
        const editor = ['DepartmentId', 'WorkerType'].includes(type.value) ? type.value : 'text';
        document.querySelectorAll('[data-rule-editor]').forEach(panel => {
            const active = panel.dataset.ruleEditor === editor;
            panel.hidden = !active;
            panel.querySelectorAll('input,select').forEach(input => { input.disabled = !active; });
        });
    };
    type.addEventListener('change', update);
    update();
})();
