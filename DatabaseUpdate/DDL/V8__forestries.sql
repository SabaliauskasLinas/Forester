ALTER TABLE forestries
ADD COLUMN IF NOT EXISTS pavadinima_pilnas VARCHAR ( 255 );

CREATE INDEX ix_forestries_MuKod_PavadinimaPilnas
ON forestries (mu_kod, pavadinima_pilnas);