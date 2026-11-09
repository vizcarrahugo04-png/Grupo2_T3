// Mejora progresiva: protege de la selección accidental de un fin anterior al inicio.
(() => {
    const start = document.getElementById("StartsLocal");
    const end = document.getElementById("EndsLocal");
    if (!start || !end) return;

    const synchronize = () => {
        end.min = start.value;
        if (end.value && start.value && end.value <= start.value) {
            end.setCustomValidity("La hora de fin debe ser posterior al inicio.");
        } else {
            end.setCustomValidity("");
        }
    };
    start.addEventListener("change", synchronize);
    end.addEventListener("change", synchronize);
    synchronize();
})();
