CREATE TABLE permits_blocks_sites (
	id INT PRIMARY KEY,
	permit_block_id INT NOT NULL REFERENCES permits_blocks(id),
	site_id INT NOT NULL,
	area NUMERIC(5,2) NOT NULL,
	is_mapped BOOLEAN NOT NULL,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);