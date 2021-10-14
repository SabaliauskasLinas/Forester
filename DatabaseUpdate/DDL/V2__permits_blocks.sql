CREATE TABLE permits_blocks (
	id INT PRIMARY KEY,
	permit_id INT NOT NULL REFERENCES permits(id),
	block_id INT NOT NULL REFERENCES blocks(id),
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);