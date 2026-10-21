-- ============================================================
-- FitTrack - Carga inicial: grupos musculares + catálogo de exercícios
-- Tabelas: muscle_groups ("Id", "Name"), exercises ("Id", "Name", "MuscleGroupId", "IsActive")
-- Idempotente: pode rodar mais de uma vez sem duplicar registros.
-- Grupos pré-existentes referenciados pelo nome: Peito, Costa, Tríceps, Bíceps.
-- ============================================================

-- 1) Grupos novos (os 4 já existentes são referenciados pelo nome no passo 2)
INSERT INTO muscle_groups ("Id", "Name")
SELECT gen_random_uuid(), g
FROM (VALUES
    ('Ombros'),
    ('Quadríceps'),
    ('Posterior de Coxa'),
    ('Glúteos'),
    ('Panturrilha'),
    ('Abdômen')
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
