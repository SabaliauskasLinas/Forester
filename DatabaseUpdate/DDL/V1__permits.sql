CREATE TABLE permits (
	id SERIAL PRIMARY KEY,
	permit_number VARCHAR ( 100 ) NOT NULL,
	region VARCHAR ( 100 ) NOT NULL,
	district VARCHAR ( 150 ) NOT NULL,
	ownership_form VARCHAR ( 150 ) NOT NULL,
    cadastral_enterprise_id INT NOT NULL REFERENCES enterprises(id),
	cadastral_forestry_id INT NOT NULL REFERENCES forestries(id),
	cadastral_location VARCHAR ( 100 ),
	cadastral_block VARCHAR ( 100 ),
	cadastral_number VARCHAR ( 100 ),
	cutting_type VARCHAR ( 150 ) NOT NULL,
	valid_from DATE NOT NULL,
	valid_to DATE NOT NULL,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	updated_at TIMESTAMP
);