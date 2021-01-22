using Microsoft.AspNetCore.Mvc;
using MovieStudio.Model;
using MovieStudio.Services;
using System;
using System.Data;

namespace MovieStudio.Controllers
{
    [Route("[controller]/v1/")]
    [ApiController]

    public class MovieStudioController : ControllerBase
    {

        private readonly IMovieServices _services;

        public MovieStudioController(IMovieServices services)
        {
            _services = services;
        }

        [HttpPost]
        public ActionResult<String> Index(MovieItems movies)
        {
            var newId = _services.AddMovieItems(movies);
            return Ok("New Id:" + newId);
        }

        [HttpGet("{MovieId}")]
        public ActionResult<String> Index(int movieId)
        {
            var resultData = _services.GetMovie(movieId);
            if (resultData == string.Empty)
                return NotFound();

            return Ok(resultData);
        }

        [HttpGet]
        [Route("GetAllMovies")]
        public ActionResult<DataTable> GetAllMovies()
        {
            var dataTableResults = _services.GetAllMovies();
            return Ok(dataTableResults);
        }


    }
}
