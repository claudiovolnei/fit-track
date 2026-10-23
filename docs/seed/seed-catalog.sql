-- ============================================================
-- FitTrack - Carga inicial: grupos musculares + catálogo de exercícios
-- Tabelas: muscle_groups ("Id", "Name"), exercises ("Id", "Name", "MuscleGroupId", "IsActive")
-- Idempotente: pode rodar mais de uma vez sem duplicar registros.
-- Grupos pré-existentes referenciados pelo nome: Peito, Costa, Tríceps, Bíceps.
-- 2ª carga (complemento): 5 grupos novos + 104 variações adicionais (passo 3).
-- 3ª carga (complemento): 50 variações adicionais (passo 4).
-- 4ª carga (complemento): biblioteca completa — 270 exercícios de mobilidade,
--    aeróbicos, olímpicos e avançados (passo 5) + grupo Cardio.
-- ============================================================

-- 1) Grupos. Os 4 grupos "originais" entram aqui para o script funcionar também
--    em base limpa; em base que já os tem, o guard por nome apenas os pula.
INSERT INTO muscle_groups ("Id", "Name")
SELECT gen_random_uuid(), g
FROM (VALUES
    ('Peito'),
    ('Costa'),
    ('Tríceps'),
    ('Bíceps'),
    ('Ombros'),
    ('Quadríceps'),
    ('Posterior de Coxa'),
    ('Glúteos'),
    ('Panturrilha'),
    ('Abdômen'),
    -- Grupos novos da 2ª carga (complemento)
    ('Antebraço'),
    ('Trapézio'),
    ('Lombar'),
    ('Adutores'),
    ('Abdutores'),
    -- Grupo novo da 4ª carga (aeróbicos/condicionamento/movimentos explosivos)
    ('Cardio')
) AS grupos(g)
WHERE NOT EXISTS (SELECT 1 FROM muscle_groups m WHERE m."Name" = grupos.g);

-- 2) Exercícios vinculados ao grupo pelo nome
INSERT INTO exercises ("Id", "Name", "MuscleGroupId", "IsActive")
SELECT gen_random_uuid(), e.nome, mg."Id", true
FROM (VALUES
    -- Peito
    ('Supino Reto com Barra',                  'Peito'),
    ('Supino Reto com Halteres',               'Peito'),
    ('Supino Inclinado com Barra',             'Peito'),
    ('Supino Inclinado com Halteres',          'Peito'),
    ('Supino Declinado com Barra',             'Peito'),
    ('Crucifixo com Halteres',                 'Peito'),
    ('Crucifixo Inclinado com Halteres',       'Peito'),
    ('Crucifixo na Máquina (Peck Deck)',       'Peito'),
    ('Crossover na Polia',                     'Peito'),
    ('Flexão de Braço',                        'Peito'),
    ('Mergulho nas Paralelas',                 'Peito'),

    -- Costa
    ('Barra Fixa',                             'Costa'),
    ('Puxada Frontal na Polia Alta',           'Costa'),
    ('Remada Curvada com Barra',               'Costa'),
    ('Remada Unilateral com Halter (Serrote)', 'Costa'),
    ('Remada Baixa na Polia',                  'Costa'),
    ('Remada Cavalinho',                       'Costa'),
    ('Remada na Máquina com Apoio no Peito',   'Costa'),
    ('Pullover com Halter',                    'Costa'),

    -- Tríceps
    ('Tríceps Testa com Barra W',              'Tríceps'),
    ('Tríceps na Polia com Barra',             'Tríceps'),
    ('Tríceps na Polia com Corda',             'Tríceps'),
    ('Tríceps Francês com Halteres',           'Tríceps'),
    ('Tríceps Coice com Halter',               'Tríceps'),
    ('Mergulho no Banco',                      'Tríceps'),
    ('Supino Reto com Pegada Fechada',         'Tríceps'),
    ('Tríceps na Máquina',                     'Tríceps'),

    -- Bíceps
    ('Rosca Direta com Barra',                 'Bíceps'),
    ('Rosca com Barra W (EZ)',                 'Bíceps'),
    ('Rosca Alternada com Halteres',           'Bíceps'),
    ('Rosca Martelo com Halteres',             'Bíceps'),
    ('Rosca Concentrada',                      'Bíceps'),
    ('Rosca Scott com Barra',                  'Bíceps'),
    ('Rosca na Polia',                         'Bíceps'),
    ('Rosca Inversa com Barra',                'Bíceps'),

    -- Ombros
    ('Desenvolvimento Militar com Barra',      'Ombros'),
    ('Desenvolvimento com Halteres',           'Ombros'),
    ('Desenvolvimento Arnold',                 'Ombros'),
    ('Elevação Lateral com Halteres',          'Ombros'),
    ('Elevação Lateral na Máquina',            'Ombros'),
    ('Elevação Frontal com Halteres',          'Ombros'),
    ('Crucifixo Inverso com Halteres',         'Ombros'),
    ('Crucifixo Inverso na Máquina',           'Ombros'),
    ('Remada Alta com Barra',                  'Ombros'),

    -- Quadríceps
    ('Agachamento Livre',                      'Quadríceps'),
    ('Agachamento Frontal',                    'Quadríceps'),
    ('Leg Press 45°',                          'Quadríceps'),
    ('Hack Squat',                             'Quadríceps'),
    ('Cadeira Extensora',                      'Quadríceps'),
    ('Afundo com Halteres',                    'Quadríceps'),
    ('Agachamento Búlgaro',                    'Quadríceps'),

    -- Posterior de Coxa
    ('Stiff com Barra',                        'Posterior de Coxa'),
    ('Levantamento Terra Romeno',              'Posterior de Coxa'),
    ('Levantamento Terra',                     'Posterior de Coxa'),
    ('Mesa Flexora (Flexora Deitado)',         'Posterior de Coxa'),
    ('Cadeira Flexora (Flexora Sentado)',      'Posterior de Coxa'),
    ('Bom Dia com Barra',                      'Posterior de Coxa'),

    -- Glúteos
    ('Elevação Pélvica (Hip Thrust)',          'Glúteos'),
    ('Agachamento Sumô',                       'Glúteos'),
    ('Coice na Polia',                         'Glúteos'),
    ('Abdutora na Máquina',                    'Glúteos'),

    -- Panturrilha
    ('Elevação de Panturrilha em Pé',          'Panturrilha'),
    ('Elevação de Panturrilha Sentado',        'Panturrilha'),
    ('Panturrilha no Leg Press',               'Panturrilha'),

    -- Abdômen
    ('Prancha Isométrica',                     'Abdômen'),
    ('Abdominal Crunch',                       'Abdômen'),
    ('Abdominal na Máquina',                   'Abdômen'),
    ('Abdominal na Polia com Corda',           'Abdômen'),
    ('Elevação de Pernas na Barra Fixa',       'Abdômen'),
    ('Elevação de Quadril (Infra)',            'Abdômen'),
    ('Russian Twist',                          'Abdômen'),
    ('Oblíquo na Polia (Torso)',               'Abdômen')
) AS e(nome, grupo)
JOIN muscle_groups mg ON mg."Name" = e.grupo
WHERE NOT EXISTS (SELECT 1 FROM exercises x WHERE x."Name" = e.nome);

-- 3) Complemento: 104 variações adicionais (2ª carga)
INSERT INTO exercises ("Id", "Name", "MuscleGroupId", "IsActive")
SELECT gen_random_uuid(), e.nome, mg."Id", true
FROM (VALUES
    -- Peito
    ('Supino Reto na Máquina Smith',           'Peito'),
    ('Supino Inclinado na Máquina Smith',      'Peito'),
    ('Supino Declinado com Halteres',          'Peito'),
    ('Supino na Máquina',                      'Peito'),
    ('Supino no Chão com Halteres',            'Peito'),
    ('Crucifixo na Polia Baixa',               'Peito'),
    ('Flexão com Pés Elevados',                'Peito'),
    ('Flexão Diamante',                        'Peito'),
    ('Flexão Pliométrica',                     'Peito'),
    ('Flexão no TRX',                          'Peito'),
    ('Pullover na Máquina',                    'Peito'),

    -- Costa
    ('Barra Fixa Supinada',                    'Costa'),
    ('Barra Fixa com Pegada Neutra',           'Costa'),
    ('Puxada com Pegada Neutra',               'Costa'),
    ('Puxada Unilateral na Polia',             'Costa'),
    ('Remada Curvada com Halteres',            'Costa'),
    ('Remada Unilateral na Máquina',           'Costa'),
    ('Remada no TRX',                          'Costa'),
    ('Remada Renegada com Halteres',           'Costa'),
    ('Remada Invertida',                       'Costa'),
    ('Pullover na Polia',                      'Costa'),

    -- Tríceps
    ('Tríceps Testa com Halteres',             'Tríceps'),
    ('Tríceps Testa Unilateral com Halter',    'Tríceps'),
    ('Tríceps Testa na Polia',                 'Tríceps'),
    ('Tríceps Francês com Barra W',            'Tríceps'),
    ('Tríceps na Polia Acima da Cabeça com Corda', 'Tríceps'),
    ('Tríceps Coice na Polia',                 'Tríceps'),
    ('Tríceps Unilateral na Polia',            'Tríceps'),
    ('Tríceps na Polia com Triângulo',         'Tríceps'),

    -- Bíceps
    ('Rosca Direta com Halteres',              'Bíceps'),
    ('Rosca 21 com Barra',                     'Bíceps'),
    ('Rosca 21 com Halteres',                  'Bíceps'),
    ('Rosca Inclinada com Halteres',           'Bíceps'),
    ('Rosca Drag com Barra',                   'Bíceps'),
    ('Rosca Zottman',                          'Bíceps'),
    ('Rosca Scott com Halteres',               'Bíceps'),
    ('Rosca Scott na Máquina',                 'Bíceps'),
    ('Rosca Unilateral na Polia',              'Bíceps'),

    -- Ombros
    ('Desenvolvimento Sentado com Barra',      'Ombros'),
    ('Desenvolvimento Sentado com Halteres',   'Ombros'),
    ('Desenvolvimento na Máquina Smith',       'Ombros'),
    ('Desenvolvimento no Landmine',            'Ombros'),
    ('Push Press com Barra',                   'Ombros'),
    ('Elevação Lateral na Polia',              'Ombros'),
    ('Elevação Frontal na Polia',              'Ombros'),
    ('Crucifixo Inverso na Polia',             'Ombros'),
    ('Face Pull na Polia',                     'Ombros'),
    ('Rotação Externa na Polia',               'Ombros'),

    -- Trapézio
    ('Encolhimento com Barra',                 'Trapézio'),
    ('Encolhimento com Halteres',              'Trapézio'),
    ('Encolhimento na Máquina',                'Trapézio'),
    ('Encolhimento na Polia',                  'Trapézio'),

    -- Quadríceps
    ('Agachamento com Halteres',               'Quadríceps'),
    ('Agachamento Goblet',                     'Quadríceps'),
    ('Agachamento Búlgaro com Barra',          'Quadríceps'),
    ('Agachamento na Máquina Smith',           'Quadríceps'),
    ('Leg Press Horizontal',                   'Quadríceps'),
    ('Cadeira Extensora Unilateral',           'Quadríceps'),
    ('Avanço Caminhando com Halteres',         'Quadríceps'),
    ('Agachamento com Salto',                  'Quadríceps'),

    -- Posterior de Coxa
    ('Mesa Flexora Unilateral',                'Posterior de Coxa'),
    ('Cadeira Flexora Unilateral',             'Posterior de Coxa'),
    ('Flexora em Pé na Máquina',               'Posterior de Coxa'),
    ('Curl Nórdico',                           'Posterior de Coxa'),
    ('Levantamento Terra Romeno com Halteres', 'Posterior de Coxa'),
    ('Stiff Unilateral com Halteres',          'Posterior de Coxa'),

    -- Glúteos
    ('Elevação Pélvica com Barra',             'Glúteos'),
    ('Elevação Pélvica na Máquina',            'Glúteos'),
    ('Elevação Pélvica Unilateral',            'Glúteos'),
    ('Afundo Cruzado (Curtsy)',                'Glúteos'),
    ('Afundo Lateral com Halteres',            'Glúteos'),
    ('Afundo Reverso com Halteres',            'Glúteos'),
    ('Levantamento Terra Sumô',                'Glúteos'),
    ('Swing com Kettlebell',                   'Glúteos'),
    ('Step Up no Bancão com Halteres',         'Glúteos'),

    -- Adutores
    ('Cadeira Adutora',                        'Adutores'),
    ('Adução de Quadril na Polia',             'Adutores'),
    ('Agachamento Cossaco',                    'Adutores'),

    -- Abdutores
    ('Abdução de Quadril na Polia',            'Abdutores'),
    ('Caminhada Lateral com Elástico',         'Abdutores'),
    ('Abdução Deitado com Elástico (Clamshell)', 'Abdutores'),

    -- Lombar
    ('Hiperextensão no Banco Romano',          'Lombar'),
    ('Extensão Lombar na Máquina',             'Lombar'),
    ('Extensão de Coluna no Solo (Superman)',  'Lombar'),

    -- Panturrilha
    ('Panturrilha em Pé na Máquina',           'Panturrilha'),
    ('Panturrilha em Pé na Máquina Smith',     'Panturrilha'),
    ('Panturrilha Unilateral em Pé',           'Panturrilha'),
    ('Panturrilha Sentado Unilateral',         'Panturrilha'),
    ('Panturrilha Tipo Burro (Donkey)',        'Panturrilha'),

    -- Abdômen
    ('Prancha Lateral',                        'Abdômen'),
    ('Abdominal Bicicleta',                    'Abdômen'),
    ('Abdominal Reverso',                      'Abdômen'),
    ('Abdominal V-Up',                         'Abdômen'),
    ('Elevação de Joelhos na Barra Fixa',      'Abdômen'),
    ('Escalador',                              'Abdômen'),
    ('Dead Bug',                               'Abdômen'),
    ('Roda Abdominal',                         'Abdômen'),
    ('Crunch Oblíquo',                         'Abdômen'),
    ('Press Pallof na Polia',                  'Abdômen'),

    -- Antebraço
    ('Rosca de Punho com Barra',               'Antebraço'),
    ('Rosca de Punho com Halteres',            'Antebraço'),
    ('Rosca Inversa de Punho com Barra',       'Antebraço'),
    ('Caminhada do Fazendeiro com Halteres',   'Antebraço'),
    ('Suspensão na Barra (Dead Hang)',         'Antebraço')
) AS e(nome, grupo)
JOIN muscle_groups mg ON mg."Name" = e.grupo
WHERE NOT EXISTS (SELECT 1 FROM exercises x WHERE x."Name" = e.nome);

-- 4) Complemento: 49 variações adicionais (3ª carga)
INSERT INTO exercises ("Id", "Name", "MuscleGroupId", "IsActive")
SELECT gen_random_uuid(), e.nome, mg."Id", true
FROM (VALUES
    -- Peito
    ('Crucifixo Declinado com Halteres',       'Peito'),
    ('Supino Inclinado na Polia',              'Peito'),
    ('Supino Unilateral com Halteres',         'Peito'),
    ('Mergulho nas Paralelas com Peso',        'Peito'),
    ('Flexão Inclinada (Mãos Elevadas)',       'Peito'),

    -- Costa
    ('Barra Fixa com Pegada Aberta',           'Costa'),
    ('Barra Fixa com Peso',                    'Costa'),
    ('Puxada Supinada',                        'Costa'),
    ('Puxada Atrás da Nuca',                   'Costa'),
    ('Remada Pendlay',                         'Costa'),
    ('Remada no Landmine',                     'Costa'),
    ('Remada Inclinada com Halteres no Banco', 'Costa'),

    -- Tríceps
    ('Tríceps na Polia com Pegada Supinada',   'Tríceps'),
    ('JM Press com Barra',                     'Tríceps'),
    ('Tríceps Francês na Máquina Smith',       'Tríceps'),
    ('Extensão de Tríceps Acima da Cabeça com Elástico', 'Tríceps'),

    -- Bíceps
    ('Rosca Inversa com Halteres',             'Bíceps'),
    ('Rosca Scott na Polia',                   'Bíceps'),
    ('Spider Curl com Barra',                  'Bíceps'),
    ('Rosca Martelo Cruzada',                  'Bíceps'),
    ('Rosca Martelo Alternada no Banco Inclinado', 'Bíceps'),

    -- Ombros
    ('Desenvolvimento Unilateral com Halter',  'Ombros'),
    ('Push Press com Halteres',                'Ombros'),
    ('Elevação Lateral Deitado com Halter',    'Ombros'),
    ('Remada Alta com Halteres',               'Ombros'),

    -- Quadríceps
    ('Avanço com Barra',                       'Quadríceps'),
    ('Afundo Goblet com Kettlebell',           'Quadríceps'),
    ('Agachamento na Caixa com Barra',         'Quadríceps'),
    ('Agachamento Landmine',                   'Quadríceps'),
    ('Agachamento Zercher',                    'Quadríceps'),
    ('Agachamento Unilateral (Pistol)',        'Quadríceps'),
    ('Leg Press Unilateral',                   'Quadríceps'),
    ('Salto no Caixote',                       'Quadríceps'),

    -- Posterior de Coxa
    ('Flexora na Polia',                       'Posterior de Coxa'),
    ('Levantamento Terra com Halteres',        'Posterior de Coxa'),
    ('Levantamento Terra Romeno na Máquina Smith', 'Posterior de Coxa'),
    ('Glute Ham Raise',                        'Posterior de Coxa'),

    -- Glúteos
    ('Elevação Pélvica na Polia',              'Glúteos'),
    ('Ponte de Glúteo',                        'Glúteos'),
    ('Agachamento com Faixa (Ativação)',       'Glúteos'),

    -- Adutores
    ('Agachamento Cossaco com Halter',         'Adutores'),
    ('Prancha de Copenhague',                  'Adutores'),

    -- Lombar
    ('Hiperextensão com Anilha',               'Lombar'),

    -- Abdômen
    ('Elevação de Pernas na Máquina',          'Abdômen'),
    ('Abdominal com Anilha',                   'Abdômen'),
    ('Rollout com Barra',                      'Abdômen'),
    ('Toque de Ombro em Prancha',              'Abdômen'),

    -- Antebraço
    ('Caminhada do Fazendeiro com Kettlebells', 'Antebraço'),
    ('Sustentação em Pinça com Anilha',        'Antebraço'),
    ('Rolo de Punho',                          'Antebraço'),

    -- Peito
    ('Mergulho assistido na máquina',          'Peito'),
    ('Mergulho nas argolas',                   'Peito'),
    ('Supino declinado no Smith',              'Peito'),
    ('Supino inclinado na máquina',            'Peito'),
    ('Supino na polia sentado',                'Peito'),
    ('Supino no chão com barra',               'Peito'),
    ('Supino no chão com kettlebell',          'Peito'),
    ('Supino Larsen com barra',                'Peito'),
    ('Supino Svend',                           'Peito'),
    ('Supino com prancha (board press) com barra', 'Peito'),
    ('Pin press com barra no rack',            'Peito'),
    ('Crossover no cabo de cima para baixo',   'Peito'),
    ('Crucifixo com faixa elástica',           'Peito'),
    ('Flexão arqueiro',                        'Peito'),
    ('Flexão com déficit',                     'Peito'),
    ('Flexão de braço na parede',              'Peito'),
    ('Flexão hindu',                           'Peito'),
    ('Passe de peito com bola medicinal',      'Peito'),
    ('Alongamento de peito na porta',          'Peito'),

    -- Costa
    ('Barra fixa arqueiro',                    'Costa'),
    ('Barra fixa assistida na máquina',        'Costa'),
    ('Barra fixa com elástico',                'Costa'),
    ('Barra fixa commando',                    'Costa'),
    ('Muscle up',                              'Costa'),
    ('Puxada alta ajoelhado',                  'Costa'),
    ('Puxada alta unilateral com pegada neutra', 'Costa'),
    ('Puxada alta unilateral com pegada supinada', 'Costa'),
    ('Rack pull',                              'Costa'),
    ('Pullover deitado na polia com corda',    'Costa'),
    ('Remada cavalinho com apoio no peito',    'Costa'),
    ('Remada com faixa elástica',              'Costa'),
    ('Remada com kettlebell a um braço',       'Costa'),
    ('Remada curvada na máquina Smith',        'Costa'),
    ('Remada gorila com kettlebells',          'Costa'),
    ('Remada iso lateral na máquina',          'Costa'),
    ('Remada Kroc',                            'Costa'),
    ('Remada seal com barra',                  'Costa'),
    ('Remada seal com halteres no banco inclinado', 'Costa'),
    ('Remada sentada unilateral no cabo',      'Costa'),
    ('Flexões escapulares',                    'Costa'),
    ('Rotação torácica (livro aberto)',        'Costa'),
    ('Liberação da Parte Superior das Costas com Rolo', 'Costa'),
    ('Liberação do dorsal com rolo',           'Costa'),
    ('Alongamento da cobra',                   'Costa'),
    ('Alongamento de dorsal',                  'Costa'),

    -- Ombros
    ('Desenvolvimento de ombros com faixa elástica', 'Ombros'),
    ('Desenvolvimento militar por trás da nuca sentado com barra', 'Ombros'),
    ('Desenvolvimento sentado com faixa elástica', 'Ombros'),
    ('Desenvolvimento Z com halteres',         'Ombros'),
    ('Elevação frontal com barra',             'Ombros'),
    ('Elevação em T no banco (de bruços)',     'Ombros'),
    ('Elevação em W de bruços (prone)',        'Ombros'),
    ('Elevação em Y de Bruços',                'Ombros'),
    ('Elevação em Y inclinado no cabo',        'Ombros'),
    ('Elevação em Y no TRX',                   'Ombros'),
    ('Elevação lateral na polia cruzada',      'Ombros'),
    ('Elevação lateral no landmine',           'Ombros'),
    ('Elevação lateral sentado com faixa elástica', 'Ombros'),
    ('Elevação lateral unilateral na polia',   'Ombros'),
    ('Elevação Powell',                        'Ombros'),
    ('Face pull com faixa elástica',           'Ombros'),
    ('Rotação externa deitado de lado',        'Ombros'),
    ('Rotação externa do ombro com faixa',     'Ombros'),
    ('Rotação interna do ombro com faixa',     'Ombros'),
    ('Press Cubano com Halteres',              'Ombros'),
    ('Push press com kettlebell',              'Ombros'),
    ('Remada alta com barra pegada aberta',    'Ombros'),
    ('Crucifixo invertido deitado em banco inclinado com halteres', 'Ombros'),
    ('Crucifixo invertido unilateral na polia', 'Ombros'),
    ('Flexão invertida na parede',             'Ombros'),
    ('Flexão pike',                            'Ombros'),
    ('Flexão pseudo planche',                  'Ombros'),
    ('Halo com kettlebell',                    'Ombros'),
    ('Lu Raises',                              'Ombros'),
    ('Passagem de ombros com bastão',          'Ombros'),
    ('Abertura com faixa elástica',            'Ombros'),
    ('Alongamento do dorminhoco',              'Ombros'),
    ('Caminhada com kettlebell acima da cabeça', 'Ombros'),
    ('Caminhada na parede',                    'Ombros'),
    ('Círculos com os braços',                 'Ombros'),
    ('Deslizamento de Serrátil na Parede',     'Ombros'),
    ('Deslizamento na Parede',                 'Ombros'),

    -- Trapézio
    ('Encolhimento com barra pegada aberta',   'Trapézio'),
    ('Encolhimento com barra por trás',        'Trapézio'),
    ('Encolhimento com barra trap',            'Trapézio'),
    ('Encolhimento inclinado com halteres',    'Trapézio'),
    ('Encolhimento na máquina Smith',          'Trapézio'),
    ('Encolhimento no cabo',                   'Trapézio'),
    ('Remada alta no cabo',                    'Trapézio'),
    ('High pull com kettlebell',               'Trapézio'),
    ('High pull com pegada snatch',            'Trapézio'),
    ('Extensão de pescoço deitado com anilha', 'Trapézio'),
    ('Flexão de pescoço deitado com anilha',   'Trapézio'),
    ('Flexão lateral de pescoço na máquina',   'Trapézio'),
    ('Retração do queixo',                     'Trapézio'),
    ('Rotações de pescoço',                    'Trapézio'),
    ('Alongamento lateral do pescoço',         'Trapézio'),

    -- Quadríceps
    ('Afundo no TRX',                          'Quadríceps'),
    ('Agachamento com barra de segurança',     'Quadríceps'),
    ('Agachamento com barra explosivo',        'Quadríceps'),
    ('Agachamento com cinturão na máquina',    'Quadríceps'),
    ('Agachamento com peso corporal',          'Quadríceps'),
    ('Agachamento frontal com kettlebells',    'Quadríceps'),
    ('Agachamento Pendular',                   'Quadríceps'),
    ('Agachamento pistol no TRX',              'Quadríceps'),
    ('Agachamento shrimp',                     'Quadríceps'),
    ('Agachamento sissy',                      'Quadríceps'),
    ('Agachamento sissy com cinto',            'Quadríceps'),
    ('Cadeira na parede',                      'Quadríceps'),
    ('Extensão terminal de joelho com faixa',  'Quadríceps'),
    ('Subida no caixote com barra',            'Quadríceps'),
    ('Alongamento de Flexor de Quadril Ajoelhado', 'Quadríceps'),
    ('Alongamento de quadríceps em pé',        'Quadríceps'),
    ('Alongamento do sofá',                    'Quadríceps'),

    -- Posterior de Coxa
    ('Bom dia com faixa elástica',             'Posterior de Coxa'),
    ('Bom dia na Smith',                       'Posterior de Coxa'),
    ('Flexora em pé no cabo',                  'Posterior de Coxa'),
    ('Flexão de pernas no TRX',                'Posterior de Coxa'),
    ('Flexão para frente sentado',             'Posterior de Coxa'),
    ('Levantamento terra com déficit',         'Posterior de Coxa'),
    ('Levantamento Terra com Kettlebell',      'Posterior de Coxa'),
    ('Levantamento terra com pegada de arranco', 'Posterior de Coxa'),
    ('Levantamento terra com pernas rígidas com barra', 'Posterior de Coxa'),
    ('Levantamento terra romeno com kettlebell', 'Posterior de Coxa'),
    ('Levantamento Terra Romeno no Landmine',  'Posterior de Coxa'),
    ('Levantamento terra romeno unilateral com halteres', 'Posterior de Coxa'),
    ('Levantamento terra unilateral com kettlebell', 'Posterior de Coxa'),
    ('Liberação miofascial de isquiotibiais',  'Posterior de Coxa'),
    ('Alongamento de isquiotibiais em pé',     'Posterior de Coxa'),

    -- Glúteos
    ('Afundo na máquina Smith',                'Glúteos'),
    ('Afundo reverso com barra',               'Glúteos'),
    ('Afundo reverso com peso corporal',       'Glúteos'),
    ('Agachamento búlgaro com halteres',       'Glúteos'),
    ('Agachamento búlgaro com peso corporal',  'Glúteos'),
    ('Agachamento goblet sumô',                'Glúteos'),
    ('Alongamento do pombo',                   'Glúteos'),
    ('Alongamento em figura 4 do glúteo',      'Glúteos'),
    ('Alongamento Mais Completo do Mundo',     'Glúteos'),
    ('Avanço Lateral com Peso Corporal',       'Glúteos'),
    ('Avanço spiderman',                       'Glúteos'),
    ('Círculos de quadril em pé',              'Glúteos'),
    ('Elevação de quadril B-stance com barra', 'Glúteos'),
    ('Elevação de quadril com halter',         'Glúteos'),
    ('Equilíbrio em uma perna',                'Glúteos'),
    ('Hip thrust no Smith',                    'Glúteos'),
    ('Levantamento terra com barra hexagonal', 'Glúteos'),
    ('Levantamento terra sumô com kettlebell', 'Glúteos'),
    ('Liberação dos glúteos com rolo',         'Glúteos'),
    ('Marcha em ponte de glúteo',              'Glúteos'),
    ('Pull-through no cabo',                   'Glúteos'),
    ('Rotação de quadril 90-90',               'Glúteos'),
    ('Step-up lateral',                        'Glúteos'),
    ('Step-up lateral com halteres',           'Glúteos'),
    ('Subida no caixote',                      'Glúteos'),
    ('Swing com kettlebell com um braço',      'Glúteos'),
    ('Abraço de joelho caminhando',            'Glúteos'),

    -- Adutores
    ('Adução de Copenhague',                   'Adutores'),
    ('Agachamento profundo sustentado',        'Adutores'),
    ('Alongamento borboleta sentado',          'Adutores'),

    -- Abdutores
    ('Abdução de quadril deitado de lado',     'Abdutores'),
    ('Abdução de quadril com faixa elástica',  'Abdutores'),
    ('Fire hydrant',                           'Abdutores'),

    -- Lombar
    ('Bird Dog',                               'Lombar'),
    ('Gato-camelo',                            'Lombar'),
    ('Hiperextensão no GHD',                   'Lombar'),
    ('Jefferson Curl',                         'Lombar'),
    ('Postura da criança',                     'Lombar'),
    ('Isometria superman',                     'Lombar'),
    ('Hiperextensão Reversa',                  'Lombar'),

    -- Panturrilha
    ('Alongamento de Panturrilha em Pé',       'Panturrilha'),
    ('Alongamento de tornozelo joelho à parede', 'Panturrilha'),
    ('Caminhada na ponta dos pés',             'Panturrilha'),
    ('Caminhada nos Calcanhares',              'Panturrilha'),
    ('Círculos de Tornozelo',                  'Panturrilha'),
    ('Elevação de panturrilha em pé com halteres', 'Panturrilha'),
    ('Elevação de Panturrilhas na Leg Press Sentado', 'Panturrilha'),
    ('Elevação de Panturrilhas Sentado com Halteres', 'Panturrilha'),
    ('Elevação do tibial anterior',            'Panturrilha'),
    ('Liberação de panturrilha com rolo',      'Panturrilha'),

    -- Abdômen
    ('Abdominal com bola medicinal',           'Abdômen'),
    ('Abdominal com pernas elevadas',          'Abdômen'),
    ('Abdominal cruzado',                      'Abdômen'),
    ('Bandeira do dragão',                     'Abdômen'),
    ('Caminhada com sandbag',                  'Abdômen'),
    ('Caminhada da mala com kettlebell',       'Abdômen'),
    ('Caminhada do fazendeiro unilateral com halter', 'Abdômen'),
    ('Dead bug com bola de estabilidade',      'Abdômen'),
    ('Elevação de tronco com pernas elevadas', 'Abdômen'),
    ('Elevação de tronco com pernas elevadas e caneleiras', 'Abdômen'),
    ('Elevação oblíqua de joelhos suspenso',   'Abdômen'),
    ('Fallout no TRX',                         'Abdômen'),
    ('Flutter Kicks (chute alternado)',        'Abdômen'),
    ('Giro russo com kettlebell',              'Abdômen'),
    ('Hollow Rock (balanço côncavo)',          'Abdômen'),
    ('Inclinação Lateral em Pé',               'Abdômen'),
    ('Isometria hollow body',                  'Abdômen'),
    ('L-Sit (sustentação em L)',               'Abdômen'),
    ('Limpador de para-brisa deitado',         'Abdômen'),
    ('Prancha com saltos (plank jacks)',       'Abdômen'),
    ('Prancha lateral com elevação de quadril', 'Abdômen'),
    ('Press Pallof ajoelhado',                 'Abdômen'),
    ('Rotação no landmine',                    'Abdômen'),
    ('Rotações de tronco',                     'Abdômen'),
    ('Scissor Kicks (chute em tesoura)',       'Abdômen'),
    ('Toes-to-Bar (pés na barra)',             'Abdômen'),
    ('Toque nos pés',                          'Abdômen'),
    ('Torção espinhal deitada',                'Abdômen'),
    ('Turkish get-up com kettlebell',          'Abdômen'),
    ('Windmill com kettlebell',                'Abdômen'),
    ('Flexão lateral com halter',              'Abdômen'),
    ('Lenhador na polia',                      'Abdômen'),

    -- Antebraço
    ('Alongamento dos flexores do punho',      'Antebraço'),
    ('Caminhada do fazendeiro com barra hexagonal', 'Antebraço'),
    ('Círculos de punho',                      'Antebraço'),
    ('Flexão de punho reversa com halteres',   'Antebraço'),

    -- Tríceps
    ('Extensão de tríceps no TRX',             'Tríceps'),
    ('JM press com halteres',                  'Tríceps'),
    ('Supino com halteres pegada fechada',     'Tríceps'),
    ('Tate Press',                             'Tríceps'),
    ('Tríceps na polia inclinado',             'Tríceps'),
    ('Coice de tríceps com halteres deitado',  'Tríceps'),
    ('Alongamento de Tríceps Acima da Cabeça', 'Tríceps'),

    -- Bíceps
    ('Rosca Bayesiana',                        'Bíceps'),
    ('Rosca Bayesian unilateral no cabo',      'Bíceps'),
    ('Rosca bíceps no cabo com corda',         'Bíceps'),
    ('Rosca de bíceps no TRX',                 'Bíceps'),
    ('Rosca inclinada na polia',               'Bíceps'),
    ('Spider curl com halteres',               'Bíceps'),

    -- Cardio (aeróbicos, condicionamento, saltos e movimentos olímpicos)
    ('Esteira',                                'Cardio'),
    ('Bicicleta ergométrica',                  'Cardio'),
    ('Elíptico',                               'Cardio'),
    ('Escada / Stair climber',                 'Cardio'),
    ('Máquina de remo',                        'Cardio'),
    ('Empurrada de trenó',                     'Cardio'),
    ('Arrasto de trenó',                       'Cardio'),
    ('Corrida vaivém',                         'Cardio'),
    ('Joelhos altos',                          'Cardio'),
    ('A-Skip',                                 'Cardio'),
    ('Polichinelos',                           'Cardio'),
    ('Pular corda',                            'Cardio'),
    ('Saltos pogo',                            'Cardio'),
    ('Burpees',                                'Cardio'),
    ('Burpee com salto na caixa',              'Cardio'),
    ('Caminhada do urso',                      'Cardio'),
    ('Caminhada do caranguejo',                'Cardio'),
    ('Inchworm (minhoca)',                     'Cardio'),
    ('Cordas de batalha',                      'Cardio'),
    ('Salto agrupado',                         'Cardio'),
    ('Salto em afundo',                        'Cardio'),
    ('Salto em profundidade',                  'Cardio'),
    ('Salto horizontal',                       'Cardio'),
    ('Salto do patinador',                     'Cardio'),
    ('Chute no glúteo',                        'Cardio'),
    ('Balanço de Pernas',                      'Cardio'),
    ('Soldadinho (chute reto)',                'Cardio'),
    ('Arremesso de bola medicinal',            'Cardio'),
    ('Arremesso acima da cabeça com bola medicinal', 'Cardio'),
    ('Arremesso rotacional com bola medicinal', 'Cardio'),
    ('Arremessos de wall ball',                'Cardio'),
    ('Thruster com barra',                     'Cardio'),
    ('Thruster com halteres',                  'Cardio'),
    ('Clean com halteres',                     'Cardio'),
    ('Clean com kettlebell a um braço',        'Cardio'),
    ('Clean com sandbag',                      'Cardio'),
    ('Power clean com barra',                  'Cardio'),
    ('Power snatch com barra',                 'Cardio'),
    ('Snatch com halter',                      'Cardio'),
    ('Arranco com kettlebell a um braço',      'Cardio'),
    ('Push jerk com barra',                    'Cardio'),
    ('Split jerk com barra',                   'Cardio')
) AS e(nome, grupo)
JOIN muscle_groups mg ON mg."Name" = e.grupo
WHERE NOT EXISTS (SELECT 1 FROM exercises x WHERE x."Name" = e.nome);
