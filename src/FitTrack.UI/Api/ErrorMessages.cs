namespace FitTrack.UI.Api;

/// <summary>
/// Translates into Portuguese the reason the API returns. The API speaks English by contract — the <c>title</c> is
/// the rule code and the <c>detail</c> the text —, so the screen chooses how to tell it to whoever is
/// training. A code without a translation falls back to the server text: better in English than hiding
/// the reason.
/// </summary>
public static class ErrorMessages
{
    private static readonly Dictionary<string, string> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        // Shared list contract
        ["search.too_long"] = "A busca deve ter no máximo 80 caracteres.",
        ["pagination.invalid"] = "A paginação informada é inválida.",

        // Workout series
        ["workout_series.name.required"] = "Informe o nome da série.",
        ["workout_series.name.too_long"] = "O nome da série é longo demais.",
        ["workout_series.name.duplicate"] = "Você já tem uma série com esse nome.",
        ["workout_series.trainer.required"] = "A série precisa de um personal.",
        ["workout_series.workout.name.required"] = "Informe o nome do treino.",
        ["workout_series.workout.name.too_long"] = "O nome do treino é longo demais.",
        ["workout_series.workout.limit_reached"] =
            "Uma série tem no máximo 5 treinos (do A ao E). Remova um treino para acrescentar outro.",
        ["workout_series.workout.not_found"] = "Esse treino não faz mais parte da série.",
        ["workout_series.workout.exercises.empty"] = "O treino precisa de pelo menos um exercício prescrito.",
        ["workout_series.exercise.required"] = "Escolha o exercício.",
        ["workout_series.exercise.duplicate"] = "Esse exercício já está na prescrição do treino.",
        ["workout_series.exercise.prescription.invalid"] =
            "Revise a prescrição: séries (1–20), repetições (1–100), carga (≥ 0) e descanso (0–600 s).",
        ["workout_series.assignment.athlete.required"] = "Escolha o atleta da série.",
        ["workout_series.assignment.expired"] =
            "A série venceu, então não dá para começar um treino novo. Um treino já iniciado ainda pode ser finalizado.",
        ["workout_series.assignment.running"] =
            "O atleta tem um treino em andamento. Finalize o treino antes de vencer a série.",

        // Today's workout
        ["session.set.workout.required"] = "Esse treino não veio de uma série.",
        ["session.set.workout.not_found"] = "O treino desta sessão não está mais disponível.",
        ["session.set.exercise.not_prescribed"] = "Esse exercício não faz parte da prescrição do treino de hoje.",
        ["session.set.after_finish"] = "O treino já foi finalizado e não aceita mais séries.",
        ["session.finish.already_finished"] = "Esse treino já foi finalizado.",
        ["session.finish.before_start"] = "O treino não pode terminar antes de começar.",
        ["session.user.required"] = "O treino precisa pertencer a um atleta.",
        ["set.exercise.required"] = "Escolha o exercício.",
        ["set.load.negative"] = "A carga não pode ser negativa.",
        ["set.repetitions.not_positive"] = "As repetições precisam ser maiores que zero.",

        // Catalog
        ["exercise.name.empty"] = "Informe o nome do exercício.",
        ["exercise.name.too_long"] = "O nome do exercício é longo demais.",
        ["exercise.muscle_group.required"] = "Escolha o grupo muscular.",
        ["muscle_group.name.required"] = "Informe o nome do grupo muscular.",
        ["muscle_group.name.too_long"] = "O nome do grupo muscular é longo demais.",
        ["muscle_group.name.duplicate"] = "Já existe um grupo muscular com esse nome.",
        ["exercise.in_use"] =
            "Exercício em uso: ele faz parte de uma prescrição ou de um treino registrado.",
        ["muscle_group.in_use"] = "Grupo em uso: ainda há exercícios vinculados a ele.",

        // Administration
        ["role.required"] = "Escolha pelo menos um perfil.",
        ["role.unknown"] = "Esse perfil não existe.",

        // Roster (vínculo atleta ↔ personal)
        ["roster.busy"] = "Esse atleta já está vinculado a outro personal.",
        ["roster.trainer.required"] = "Escolha o personal que vai receber o atleta.",

        // Avatar
        ["avatar.too_large"] = "A imagem deve ter no máximo 2 MB.",
        ["avatar.type.invalid"] = "Aceitamos apenas imagens JPEG, PNG ou WebP.",

        // Password
        ["password.same"] = "A nova senha deve ser diferente da atual.",

        // Account and transport
        ["Invalid credentials."] = "E-mail ou senha incorretos.",
        ["not_found"] = "Não encontramos o que você procurava. Atualize a página.",
        ["forbidden"] = "Você não tem permissão para essa ação.",
        ["invalid_command"] = "Não foi possível concluir a ação com os dados informados.",
    };

    /// <summary>
    /// The text the screen shows. Validation comes first because it is the most specific: when the API
    /// refuses fields, it is the field the athlete has to correct — not the general reason.
    /// </summary>
    public static string Describe(ApiException error)
    {
        foreach (var field in error.Errors.Keys)
        {
            if (ByField.TryGetValue(field, out var message))
            {
                return message;
            }
        }

        return error.Code is { } code && ByCode.TryGetValue(code, out var byCode)
            ? byCode
            : error.Message;
    }

    /// <summary>
    /// Keys of the validation problems: registration uses the Identity codes and the name comes from the
    /// field itself. A field without a translation falls through to the server message.
    /// </summary>
    private static readonly Dictionary<string, string> ByField = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "Informe seu nome.",
        ["Email"] = "Revise o e-mail informado.",
        ["InvalidEmail"] = "Informe um e-mail válido.",
        ["DuplicateEmail"] = "Esse e-mail já está cadastrado.",
        ["DuplicateUserName"] = "Esse e-mail já está cadastrado.",
        ["Password"] = "Revise a senha informada.",
        ["PasswordTooShort"] = "A senha precisa de pelo menos 8 caracteres.",
        ["PasswordRequiresDigit"] = "A senha precisa ter um número.",
        ["PasswordRequiresLower"] = "A senha precisa ter uma letra minúscula.",
        ["PasswordRequiresUpper"] = "A senha precisa ter uma letra maiúscula.",
        ["PasswordRequiresNonAlphanumeric"] = "A senha precisa ter um caractere especial.",
        ["PasswordRequiresUniqueChars"] = "A senha precisa de mais caracteres diferentes.",
        ["PasswordMismatch"] = "A senha atual está incorreta.",
    };
}
