window.fitTrack = window.fitTrack || {};

// O Enter escolhe a opção ativa do combobox. Sem impedir o comportamento padrão, o formulário ao
// redor seria enviado antes de o Blazor tratar a tecla. O listener fica no próprio elemento e é
// registrado uma única vez por instância.
window.fitTrack.preventComboboxEnter = function (element) {
    if (!element || element.dataset.comboboxEnterGuard === 'true') {
        return;
    }

    element.dataset.comboboxEnterGuard = 'true';
    element.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
        }
    });
};
