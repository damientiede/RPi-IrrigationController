<html>
   <head>
      <style type="text/css">
         body { font-family: Arial, Helvetica, sans-serif; font-size:13px; color:#222426; background:#ffffff; }
         table { padding:5px; }
         td { padding:5px; }
      </style>
      <script type="text/javascript">
	function init() {
	   var $qs = getParameterByName("et");
	   if ($qs.indexOf("1") > -1) { $('#chkApplication').prop('checked',true); }
	   if ($qs.indexOf("2") > -1) { $('#chkFault').prop('checked',true); }
	   if ($qs.indexOf("3") > -1) { $('#chkIO').prop('checked',true); }
	}
        function r() { 
          var $url = 'events.php';
	  var et=[];
          if ($('#chkApplication').is(':checked')) {
            et.push('1');
          }
	  if ($('#chkFault').is(':checked')) {
            et.push('2');
          }
	  if ($('#chkIO').is(':checked')) {
            et.push('3');
          }
          var qs = '';
	  if (et.length == 1) { qs = et[0]; }
          if (et.length == 2) { qs = et[0]+','+et[1]; }
	  if (et.length == 3) { qs = et[0]+','+et[1]+','+et[2]; }
	  if (et.length > 0) { $url = $url + '?et='+qs; }
	  window.location.href=$url;
        }

      </script>
      <script type="text/javascript" src="js/jquery-3.1.1.min.js"></script>
      <script type="text/javascript" src="js/ic.js"></script>
   </head>
   <body onload="init()">
      <h1>Event history</h1>
      <div>
         <input type="checkbox" id="chkApplication"></input> Application
         <input type="checkbox" id="chkFault"></input> Fault
         <input type="checkbox" id="chkIO"></input> IO
         <a href="#" class="button" id="pbRefresh" onclick="r();">Refresh</a>
      </div>
      <hr/>
<?php    
   $conn = new mysqli("localhost","root","raspberry","IrrigationController");
   if ($conn->connect_error) {
      echo("Connection failed " . $conn->connect_error);
   }
   $sql = "SELECT * from EventHistory Order By TimeStamp DESC LIMIT 100";
   $et = "(1,2,3)";
   if (!empty($_GET["et"])) {
	$et = sprintf("(%s)",$_GET["et"]);
	$sql = sprintf("SELECT * from EventHistory where EventType in %s Order By TimeStamp DESC LIMIT 100",$et);
   }
   echo($sql);
   $result = $conn->query($sql);

   if ($result->num_rows > 0) {
      while($row = $result->fetch_assoc()) {
         echo  $row["TStamp"]. " - " . $row["Description"] . "<br/>";
      }
   }
   else
   {
      echo "0 results";
   }
   $conn->close();



?>
   </body>
</html>

