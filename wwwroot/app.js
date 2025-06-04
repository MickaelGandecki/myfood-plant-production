function PreventDefaultEnterHandler(element) {
    element.addEventListener('keydown', function (e) {
        if (e.key === "Enter" && !e.altKey && !e.shiftKey) {
            e.preventDefault();
        }
    });
}