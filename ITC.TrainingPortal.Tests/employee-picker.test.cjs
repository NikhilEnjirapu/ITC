const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function fixture(size = 3) {
    const node = (extra = {}) => ({ value: '', hidden: false, disabled: false, checked: false, textContent: '',
        events: {}, addEventListener(event, fn) { this.events[event] = fn; }, ...extra });
    const elements = Object.fromEntries(['employee-department', 'employee-search', 'employee-selection-count',
        'employee-no-matches', 'employee-filter-panel', 'employee-csv-panel', 'csvFile',
        'employee-select-matching', 'employee-clear'].map(id => [id, node()]));
    const radios = [node({ value: 'filter', checked: true }), node({ value: 'csv' })];
    const rows = Array.from({ length: size }, (_, i) => {
        const box = node({ disabled: size === 3 && i === 2 });
        return node({ dataset: { department: i === 0 ? '1' : '2', search: `employee ${i}` },
            classList: { toggle() {} }, querySelector() { return box; } });
    });
    elements['employee-picker'] = node({
        querySelectorAll(selector) { return selector === '.employee-option' ? rows : radios; },
        querySelector() { return radios.find(radio => radio.checked); }
    });
    vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../ITC.TrainingPortal.Web/wwwroot/js/employee-picker.js'), 'utf8'), {
        document: { getElementById: id => elements[id] }
    });
    return { elements, rows, radios };
}

test('select all applies to matching eligible employees and preserves earlier selections', () => {
    const { elements, rows } = fixture();
    elements['employee-department'].value = '1';
    elements['employee-department'].events.change();
    elements['employee-select-matching'].events.click();
    assert.deepEqual(rows.map(row => row.querySelector().checked), [true, false, false]);
    elements['employee-department'].value = '2';
    elements['employee-department'].events.change();
    elements['employee-select-matching'].events.click();
    assert.deepEqual(rows.map(row => row.querySelector().checked), [true, true, false]);
    elements['employee-search'].value = 'not found';
    elements['employee-search'].events.input();
    assert.equal(elements['employee-no-matches'].hidden, false);
    elements['employee-clear'].events.click();
    assert.equal(rows.some(row => row.querySelector().checked), false);
});

test('CSV mode requires the file and switches away from filter controls', () => {
    const { elements, radios } = fixture();
    assert.equal(elements.csvFile.disabled, true);
    radios[0].checked = false;
    radios[1].checked = true;
    radios[1].events.change();
    assert.equal(elements['employee-filter-panel'].hidden, true);
    assert.equal(elements['employee-csv-panel'].hidden, false);
    assert.equal(elements.csvFile.required, true);
});

test('select all refuses oversized groups without silently selecting a partial group', () => {
    const { elements, rows } = fixture(1001);
    elements['employee-select-matching'].events.click();
    assert.equal(rows.some(row => row.querySelector().checked), false);
    assert.match(elements['employee-selection-count'].textContent, /Narrow the filters/);
});
