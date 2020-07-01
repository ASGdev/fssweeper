const fs = require('fs');
const readline = require('readline');
const { exec } = require('child_process');
const commandLineArgs = require('command-line-args')
const argsDefinitions = [
	{ name: 'dir', alias: 'd', type: String },
	{ name: 'opened', alias: 'o', type: String },
	{ name: 'on', type: String },
	{ name: 'off', type: String }
]
const args = commandLineArgs(argsDefinitions)

let parsed = []
let line_number = 0;
let bool1 = false;
let currentEl = {};
let targetPattern = /^LE_.*$/;
let opened = [];
let rlcValues = {
		on: {
			r: 1,
			l: 1,
			c: 1
		},
		off: {
			r: 1,
			l: 1,
			c: 1
		}
	}

function summarize(){
	console.log("Project root dir : " + args.dir);
	
	// process opened
	opened = args.opened.split(',');
	
	console.log(opened.length + " opened gates : ");
	console.log(opened);
	
	// process rlc values
	_v = args.on.split(',');
	_v.forEach(prop => {
		_p = prop.split('=');
		
		switch (_p[0]){
			case 'r':
				rlcValues.on.r = _p[1];
				break
			case 'l':
				rlcValues.on.l = _p[1];
				break
			case 'c':
				rlcValues.on.c = _p[1];
				break
			default :
				process.exit(-1);
		}
	});
	
	_v = args.off.split(',');
	_v.forEach(prop => {
		_p = prop.split('=');
		
		switch (_p[0]){
			case 'r':
				rlcValues.off.r = _p[1];
				break
			case 'l':
				rlcValues.off.l = _p[1];
				break
			case 'c':
				rlcValues.off.c = _p[1];
				break
			default :
				process.exit(-1);
		}
	});
	
	console.log(rlcValues);
	
	parse();
}

function parse(){
	let newFile = "";
	
	const rl = readline.createInterface({
	  input: fs.createReadStream(args.dir + '/Model/3D/Model.mod'),
	  crlfDelay: Infinity
	});

	rl.on('line', (line) => {
	  //console.log(line);
	  
	  line_number++;
	  
	  if(line.startsWith("With LumpedElement")){
		  bool1 = true
		  console.log("Start block")
		  
		  currentEl = {};
		  
		  newFile += line + "\n"
	  }
	  
	  else if(line.startsWith('     .SetName "') && bool1){
			const regexp = /^\s*\.SetName "(.*)"\s$/g;
			const matches = regexp.exec(line);

			currentEl.el = matches[1]
			
			if(opened.includes(currentEl.el)){ 
				console.log("Setting ON values to " + currentEl.el);
				currentEl.type = "on"
			} else {
				console.log("Setting OFF values to " + currentEl.el);
				currentEl.type = "off"
			}
			
			newFile += line + "\n"
	  }
	  
	  else if(line.startsWith('     .SetR  "') && bool1){
			const regexp = /^\s*\.SetR\s*"(.*)"\s$/g;
			const matches = regexp.exec(line);

			
			//currentEl.r = {line: line_number, value: matches[1]};
			if(currentEl.type === "off"){
				console.log("Applying off r value");
				newFile += '     .SetR  "' + rlcValues.off.r + '" ' + "\n"
			} else {
				console.log("Applying on r value");
				newFile += '     .SetR  "' + rlcValues.on.r + '" ' + "\n"
			}
			
	  }
	  
	  else if(line.startsWith('     .SetL  "') && bool1){
			const regexp = /^\s*\.SetL\s*"(.*)"\s$/g;
			const matches = regexp.exec(line);
			
			//currentEl.l = {line: line_number, value: matches[1]};
			if(currentEl.type === "off"){
				console.log("Applying off l value");
				newFile += '     .SetL  "' + rlcValues.off.l + '" ' + "\n"
			} else {
				console.log("Applying on l value");
				newFile += '     .SetL  "' + rlcValues.on.l + '" ' + "\n"
			}
	  }
	  
	  else if(line.startsWith('     .SetC  "') && bool1){
			const regexp = /^\s*\.SetC\s*"(.*)"\s$/g;
			const matches = regexp.exec(line);
			
			//currentEl.c = {line: line_number, value: matches[1]};
			if(currentEl.type === "off"){
				console.log("Applying off c value");
				newFile += '     .SetC  "' + rlcValues.off.c + '" ' + "\n"
			} else {
				console.log("Applying on c value");
				newFile += '     .SetC  "' + rlcValues.on.c + '" ' + "\n"
			}
	  }
	  
	  else if(line.startsWith("End With") && bool1){
		console.log("End block")
		bool1 = false
		
		parsed.push(currentEl)
		
		currentEl = {};
		
		newFile += line + "\n"
	  }
	  
	  else {
		  newFile += line + "\n"
	  }
		  
	});

	rl.on('close', () => {
	  fs.writeFile(args.dir + '/Model/3D/Model.mod', newFile, (err) => {
		  if (err) throw err;
		  console.log('The file has been saved!');
		});
	});
}



setTimeout(function() {
    console.log('End');
}, 30000);

summarize();



