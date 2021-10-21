CREATE TABLE permits_blocks (
	id SERIAL PRIMARY KEY,
	permit_id INT NOT NULL REFERENCES permits(id),
	cadastral_block_id INT NOT NULL REFERENCES blocks(id),
	has_unmapped_sites BOOLEAN NOT NULL DEFAULT FALSE,
	created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);