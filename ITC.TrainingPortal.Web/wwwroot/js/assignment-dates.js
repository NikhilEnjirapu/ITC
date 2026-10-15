(() => {
    const assigned = document.getElementById("AssignedAt");
    const due = document.getElementById("DueDate");
    const updateMinimum = () => { due.min = assigned.value; };
    document.getElementById("assignment-now").addEventListener("click", () => {
        // Use IST independently of the browser or server's configured timezone.
        assigned.value = new Date(Date.now() + 330 * 60 * 1000).toISOString().slice(0, 16);
        updateMinimum();
    });
    assigned.addEventListener("change", updateMinimum);
    updateMinimum();
})();
