CREATE TABLE permits (
	id INT PRIMARY KEY,
	permit_number VARCHAR ( 100 ) NOT NULL,
	region VARCHAR ( 100 ) NOT NULL,
	district VARCHAR ( 150 ) NOT NULL,
	ownership_form VARCHAR ( 150 ) NOT NULL,
    enterprise_id INT NOT NULL REFERENCES enterprises(id),
	forestry_id INT NOT NULL REFERENCES forestries(id),
	cadastral_location INT,
	cadastral_block INT,
	cadastral_number INT,
	cutting_type VARCHAR ( 150 ) NOT NULL,
	valid_from DATE NOT NULL,
	valid_to DATE NOT NULL,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);