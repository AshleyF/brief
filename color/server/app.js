"use strict";

var express = require('express');
var fs = require('fs');
var bodyParser = require('body-parser');

var app = express();

// log all requests (for now, for debugging)
app.use(function (req, res, next) {
    console.log('Request: %s (%d)', req.url, Date.now());
    next();
})

app.use(bodyParser.json());

function filePath(req) { return './../data/' + req.param('name') + '.b'; }

app.post('/store', function (req, res, next) {
    fs.writeFile(filePath(req), JSON.stringify(req.body), function (err) {
        if (err) throw err; // TODO: handle
        res.writeHead(200, {'Content-Type': 'text/javascript'});
        res.end();
        console.log('Stored data');
        next();
    });
});

app.use('/load', function (req, res, next) {
    res.writeHead(200, {'Content-Type': 'text/javascript'});
    var file = filePath(req);
    fs.exists(file, function (exists) {
        if (exists) {
            fs.readFile(file, function (err, data) {
                if (err) throw err; // TODO: handle
                res.end(data);
                console.log('Loaded data');
                next();
            });
        } else {
            res.end('[{"kind":"comment","value":"' + file + '"}]');
            console.log('New data');
            next();
        }
    });
});

// serve static client content
app.use('/', express.static(__dirname + '/../client'));

app.listen(80);
