ALTER TABLE enterprises
ADD COLUMN IF NOT EXISTS pavadinima_pilnas VARCHAR ( 255 );

CREATE INDEX ix_enterprises_PavadinimaPilnas
ON enterprises (pavadinima_pilnas);