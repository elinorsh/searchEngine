---------ReadME------------
***PART2***
This program can now return a list of relevant documents to a query.

To run a query you need to do one of the following insructions:
	1. index the corpus (with the insruction in PART1) and then enter the query.
	2. press 'load data' to choose a folder that contains: documents, dictionary and stop words.
		also, choose a folder that contains the posting files by pressing "choose posting path"

You have 2 options to excute a query:
	1. write the query in the textbox and press "find documents!"
	2. choose a file with a list of queries. PAY ATTENTION: choosing this file will excute the queries, you need to upload all the data first.
the results of the queries will appear at the bottom of the window.

few more things...
	1. you can choose if you want the queries to be with or without stemmer by checking/unchecking the checkbox.
		If you choose with stemmer, you need to choose a posting folder with stemmer and a data folder with stemmer.
	2. you can choose to save the results. check the checkbox and choose where to save it. it will automaticly be named "results.txt"
	3. after loading the data, a "choose languages" button will appear. perss it to get documents only with the languages you want.
		to do that, select all the desirable languages and then press "OK".
	4. if you type a word and press space, a list of followup suggestions words will appear at the bottom of the window. 
				Notice! it's only for the first word in the query.


***PART1***
This program is indexing a corpus in a Inverted Index method.
To start this program:
	1. press 'browse' in  'Load files from' 
		to choose the folder from which the corpus will be loaded.
		Notice! the folder should also include a txt file named 'stop_words.txt', if not, an error message will pop-up.
	2. 	press 'browse' in  'Save files to'
		to choose the folder where the files will be saved.
		The files are: A folder of posting files, a dictionary file and a documents file.
	3.	Decide if you want stemmer or not by checking/unchecking the stemmer checkbox.
	4. press 'Go!'
		when the frogram done indexing a pop-up window with summary information will appear.
other options:
	1. 'Load Dictionary' will allow you to load a txt file dictionary to the memory.
		Notice! the file has to be in the right pattern.
	2.	'Show Dictionary' will reveal a list of all the words in the memory and thier sum of appearances.
	3.	'clear all' will clear all the memory of the program and will delete all the files.
	
Enjoy!
Noga&Elinor :)
	