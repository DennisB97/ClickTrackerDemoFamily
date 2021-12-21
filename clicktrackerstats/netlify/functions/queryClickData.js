const axios = require("axios");

exports.handler = async function (event, context) {
 
    try {
      const response = await axios.get(process.env.REACT_APP_API_KEY + "uniqueString=" + event.queryStringParameters.uniqueString + "&" 
    + "startDate=" + event.queryStringParameters.startDate + "&" 
    + "endDate=" + event.queryStringParameters.endDate, {
        method: 'GET', 
        mode: 'cors', 
        headers: {
            'Content-Type': 'application/json'
        },
        });
     
      return {statusCode: 200,body: JSON.stringify(response.data)};
    
    } catch (err) {
      console.log(err);
      return {statusCode: 404, body: ""};
    }
  };



