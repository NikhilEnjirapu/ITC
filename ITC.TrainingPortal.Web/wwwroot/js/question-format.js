(() => {
    const format = document.getElementById("IsTypedAnswer");
    function update() {
        const typed = format.value.toLowerCase() === "true";
        document.querySelectorAll("[data-keyword-fields]").forEach(group => {
            group.hidden = !typed;
            group.querySelectorAll("textarea").forEach(field => { field.disabled = !typed; });
        });
        document.querySelectorAll("[data-choice-fields]").forEach(group => {
            group.hidden = typed;
            group.querySelectorAll("input, select").forEach(field => { field.disabled = typed; field.required = !typed; });
        });
    }
    format.addEventListener("change", update);
    update();
})();
