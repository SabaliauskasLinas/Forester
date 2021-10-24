CREATE TABLE permits_sites (
	id SERIAL PRIMARY KEY,
	permit_block_id INT NOT NULL REFERENCES permits_blocks(id),
	cadastral_site_id INT NULL REFERENCES sites(id),
	site_codes VARCHAR ( 255 ) NULL,
	area NUMERIC(5,2) NOT NULL,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	updated_at TIMESTAMP
);