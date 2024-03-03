***********************************************************************************
// JavaScript server, runs on Raspberry Pi and handles requests from rover_interface (The C# application which runs on windows)

var http = require('http').createServer(handler); // require http server, and create server with function handler()
var fs = require('fs'); // require filesystem module
const { exec } = require('child_process'); // for the bash scripts
const readline = require('readline');


// listen on port
http.listen(8080, () => {
    console.log('Server listening on port 8080');
});

// values to pass to pathFinding
let x = '';
let y = '';
// calls the server request handler,
// res is the http response object, something back to the client. Its an instance of ServerResponse class from http module of nodejs
function handler(req, res) {
  if (req.url === '/stop') {
     stop(res);
  } else if (req.url === '/forward') {
      forward(res);
  } else if (req.url === '/reverse') {
      reverse(res);
  } else if (req.url === '/left') {
      left(res);
  } else if (req.url === '/right') {
      right(res);
  } else if (req.url === '/image') {
      returnImage(res);
  } else if (req.url === '/map') {
      returnMap(res);
  } else if (req.url === '/initialise') {
      initialiseGpios(res);
  } else if (req.url === '/findPath') {
      console.log("findpath url end received");
      let body = '';
      req.on('data', (chunk) => {
        body += chunk.toString();
      });
//      console.log(body);
      req.on('end', () => {
        const [x, y] = body.split(' ');
        console.log(`body is ${body}, x = ${x}, y = ${y}`);
        startPathFinding(x, y, res);
      });
  } else {
      res.end('Invalid endpoint');
  }
}

function startPathFinding(x, y, res) {
//  const command = `python3 /home/rover/pathfinding/getToPosition.py ${x} ${y}`;
  exec(`python3 /home/rover/pathfinding/getToPosition.py ${x} ${y}`, function(error, stdout, sterr) {
//  exec(command, function(error, stdout, sterr) {
    if (error) {
      console.log('Error starting pathfinding: ' + error);
      return;
    }
    res.end(`Pathfinding initiated with parameters x = ${x}, y = ${y}`);
    console.log("Received pathFinding command with parameters x = ${x}, y = ${y}");
  });
}

function initialiseGpios(res) {
  exec('sh /home/rover/bash/init_HB_Gpios.sh', function(error, stdout, sterr) {
    if (error) {
      console.error('exec error: ' + error);
      return;
    }
    console.log("Gpios initialsed");
    res.end('Gpios initialised\n');
  });
}

function stop(res) {
  exec('sh /home/rover/bash/stop.sh', function(error, stdout, sterr) {
    if (error) {
      console.error('exec error: ' + error);
      return;
    }
    res.end('Stopped\n');
  });
}

function forward(res) {
  exec('sh /home/rover/bash/forward.sh', function(error, stdout, sterr) {
    if (error) {
      console.error('exec error: ' + error);
      return;
    }
    res.end('Moving forward\n');
  });
}

function reverse(res) {
  exec('sh /home/rover/bash/reverse.sh', function(error, stdout, sterr) {
    if (error) {
      console.error('exec error: ' + error);
      return;
    }
    res.end('Reversing\n');
  });
}

function left(res) {
  exec('sh /home/rover/bash/left.sh', function(error, stdout, stderr) {
    if (error) {
      console.error('exec error: ' + error);
      return;
    }
    res.end('Turning left\n');
  });
}

function right(res) {
  exec('sh /home/rover/bash/right.sh', function(error, stdout, sterr) {
    if (error) {
      console.error('exec error: ' + error);
      return;
    }
    res.end('Turning right\n');
  });
}



function returnImage(res) {
  exec('fswebcam -r 1280x720 --no-banner image.jpg', function (err, stdout, stderr) {
    if (err) {
      console.error('exec fswebcam error: ${error}');
      return;
    }
    fs.readFile('image.jpg', (err, data) => {
      if (err) {
        console.error(err);
        res.statusCode = 500;
        res.end();
        return;
      }
      res.writeHead(200, { 'Content-Type': 'image/jpg' });
      res.end(data);
    });
  });
}

// run the c++ grabber, then access the file that it creates
function returnMap(res) {
  // C++ program modified to output a text file of theta and distance values
  exec('sh runSimpleGrabber.sh', function (err, stdout, stderr) {
    if (err) {
      console.error('exec simple grabber error: ${error}');
      return;
    }
    if(!err) {
      console.log("Simple grabber has run");
    }

    // take the txt file and save as a variable
    var lidarSnap = fs.readFileSync("lidar360Snap.txt").toString('utf-8');
    // repond with lidarSnap data
    res.end(lidarSnap);

  });
}
