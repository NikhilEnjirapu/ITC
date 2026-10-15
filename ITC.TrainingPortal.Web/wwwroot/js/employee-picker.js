(() => {
    const picker = document.getElementById('employee-picker');
    if (!picker) return;
    const rows = [...picker.querySelectorAll('.employee-option')];
    const department = document.getElementById('employee-department');
    const search = document.getElementById('employee-search');
    const status = document.getElementById('employee-selection-count');
    const boxes = rows.map(row => row.querySelector('input'));
    const count = () => boxes.filter(box => box.checked && !box.disabled).length;
    function filter() {
        const query = search.value.trim().toLowerCase();
        rows.forEach(row => {
            const matches = (!department.value || row.dataset.department === department.value) && row.dataset.search.includes(query);
            // Bootstrap's d-block utility overrides the hidden attribute.
            row.classList.toggle('d-block', matches);
            row.hidden = !matches;
        });
        const shown = rows.filter(row => !row.hidden).length;
        status.textContent = `${count()} selected · ${shown} matching`;
        document.getElementById('employee-no-matches').hidden = shown > 0;
    }
    picker.querySelectorAll('[name="selectionMode"]').forEach(radio => radio.addEventListener('change', mode));
    function mode() {
        const csv = picker.querySelector('[name="selectionMode"]:checked').value === 'csv';
        document.getElementById('employee-filter-panel').hidden = csv;
        document.getElementById('employee-csv-panel').hidden = !csv;
        document.getElementById('csvFile').disabled = !csv;
        document.getElementById('csvFile').required = csv;
    }
    department.addEventListener('change', filter);
    search.addEventListener('input', filter);
    boxes.forEach(box => box.addEventListener('change', () => {
        if (count() > 1000) { box.checked = false; filter(); status.textContent += ' · Limit: 1,000 employees'; }
        else filter();
    }));
    document.getElementById('employee-select-matching').addEventListener('click', () => {
        const matches = rows.filter(row => !row.hidden).map(row => row.querySelector('input')).filter(box => !box.disabled && !box.checked);
        if (count() + matches.length > 1000) { status.textContent = 'More than 1,000 employees would be selected. Narrow the filters first.'; return; }
        matches.forEach(box => { box.checked = true; });
        filter();
    });
    document.getElementById('employee-clear').addEventListener('click', () => { boxes.forEach(box => { box.checked = false; }); filter(); });
    filter();
    mode();
})();
