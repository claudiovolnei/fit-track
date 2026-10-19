namespace FitTrack.UI.Api;

/// <summary>
/// Traduz para o português o motivo que a API devolve. A API fala inglês por contrato — o <c>title</c> é
/// o código da regra e o <c>detail</c> o texto —, então quem escolhe como contar isso a quem está
/// treinando é a tela. Um código sem tradução cai no texto do servidor: melhor em inglês do que esconder
/// o motivo.
/// </summary>
public static class ErrorMessages
{
    private static readonly Dictionary<string, string> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        // Série de treino
        ["workout_series.name.required"] = "Informe o nome da série.",
        ["workout_series.name.too_long"] = "O nome da série é longo demais.",
        ["workout_series.name.duplicate"] = "Você já tem uma série com esse nome.",
        ["workout_series.trainer.required"] = "A série precisa de um personal trainer.",
        ["workout_series.workout.name.required"] = "Informe o nome do treino.",
        ["workout_series.workout.name.too_long"] = "O nome do treino é longo demais.",
        ["workout_series.workout.limit_reached"] =
            "Uma série tem no máximo 5 treinos (do A ao E). Remova um treino para acrescentar outro.",
        ["workout_series.workout.not_found"] = "Esse treino não faz mais parte da série.",
        ["workout_series.workout.exercises.empty"] = "O treino precisa de pelo menos um exercício prescrito.",
        ["workout_series.exercise.required"] = "Escolha o exercício.",
        ["workout_series.exercise.duplicate"] = "Esse exercício já está na prescrição do treino.",
        ["workout_series.exercise.prescription.invalid"] =
            "Revise a prescrição: séries, repetições e carga precisam ser positivas.",
        ["workout_series.assignment.athlete.required"] = "Escolha o atleta da série.",
        ["workout_series.assignment.expired"] =
            "A série venceu, então não dá para começar um treino novo. Um treino já iniciado ainda pode ser finalizado.",

        // Treino do dia
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

        // Catálogo
        ["exercise.name.empty"] = "Informe o nome do exercício.",
        ["exercise.name.too_long"] = "O nome do exercício é longo demais.",
        ["exercise.muscle_group.required"] = "Escolha o grupo muscular.",
        ["muscle_group.name.required"] = "Informe o nome do grupo muscular.",
        ["muscle_group.name.too_long"] = "O nome do grupo muscular é longo demais.",
        ["muscle_group.name.duplicate"] = "Já existe um grupo muscular com esse nome.",

        // Conta e transporte
        ["Invalid credentials."] = "E-mail ou senha incorretos.",
        ["not_found"] = "Não encontramos o que você procurava. Atualize a página.",
        ["forbidden"] = "Você não tem permissão para essa ação.",
        ["invalid_command"] = "Não foi possível concluir a ação com os dados informados.",
    };

    /// <summary>
    /// O texto que a tela mostra. A validação vem primeiro porque é a mais específica: quando a API
    /// recusa campos, é o campo que o atleta precisa corrigir — e não o motivo geral.
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
    /// Chaves dos problemas de validação: o cadastro usa os códigos do Identity e o nome vem pelo
    /// próprio campo. Um campo sem tradução segue para a mensagem do servidor.
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
    };
}
